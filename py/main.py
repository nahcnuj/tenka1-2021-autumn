"""
実行には python3 環境が必要です。
TOKEN 変数を書き換えて実行してください。

サンプル初期実装
移動先に資源が無い回収車を、ランダムに選んだ出現中の資源へと移動させる
ただしこのとき２台以上の回収車が同じ資源を選ばないようにする
"""

import os
import random
import time
import json
from typing import Iterable, List, Set, Tuple
from urllib.request import urlopen

# ゲームサーバのアドレス / トークン
GAME_SERVER = os.getenv('GAME_SERVER', 'https://contest.2021-autumn.gbc.tenka1.klab.jp')
# GAME_SERVER = 'https://contest.2021-autumn.gbc.tenka1.klab.jp/staging'  # 開発用環境
TOKEN = os.getenv('TOKEN', 'YOUR_TOKEN')


def call_api(x: str) -> dict:
    url = f'{GAME_SERVER}{x}'
    print(url)
    with urlopen(url) as res:
        return json.loads(res.read())


def call_game() -> dict:
    return call_api(f'/api/game/{TOKEN}')


def call_move(index: int, x: int, y: int) -> dict:
    return call_api(f'/api/move/{TOKEN}/{index}-{x}-{y}')


def call_will_move(index: int, x: int, y: int, t: int) -> dict:
    return call_api(f'/api/will_move/{TOKEN}/{index}-{x}-{y}-{t}')


def call_resources(ids: Iterable[int]) -> dict:
    return call_api(f'/api/resources/{TOKEN}/{"-".join(map(str, ids))}')


def calc_score(game) -> float:
    a = sorted(o["amount"] for o in game["owned_resource"])
    return a[0] + 0.1 * a[1] + 0.01 * a[2]


class Bot:
    def solve(self):
        while True:
            game = call_game()
            if game["status"] != "ok":
                print(game["status"])
                break

            print(' '.join(f'{o["type"]}: {o["amount"]:.2f}' for o in game["owned_resource"]), f'Score: {calc_score(game):.2f}')

            resource_positions: Set[Tuple[int, int]] = set()
            for r in game["resource"]:
                if r["t0"] <= game["now"] < r["t1"]:
                    resource_positions.add((r["x"], r["y"]))

            index_list: List[int] = []
            for i in range(5):
                index = i + 1
                m = game["agent"][i]["move"][-1]
                x, y = (m["x"], m["y"])
                if (x, y) in resource_positions:
                    resource_positions.remove((x, y))
                else:
                    index_list.append(index)

            for index in index_list:
                if not resource_positions:
                    break
                x, y = random.choice(list(resource_positions))
                call_move(index, x, y)
                resource_positions.remove((x, y))

            time.sleep(1.0)


if __name__ == "__main__":
    bot = Bot()
    bot.solve()
