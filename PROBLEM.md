問題概要
======

![ビジュアライザイメージ](/img/vis.png) 

これは資源回収車を操作してフィールド上に存在する資源を集め、それによって計算される得点を競うゲームです。

フィールド内には 5 台の資源回収車がいて、資源回収車はフィールド内を移動することができます。

フィールドは線分 (0, 0), (30, 30) を対角線とする正方形であり、フィールド内にはランダムに資源が出現します。

出現する資源は3種類あり、それぞれ A, B, C と英大文字で表されます。

また資源ごとに出現時刻, 消滅時刻, 回収速度が決まっています。

資源と同じ座標に資源回収車がいる間、回収速度をその座標にいる他のプレイヤーも含めた資源回収車の台数で割った速度で資源を回収できます。

回収した資源の量に応じて得点が計算され、ゲーム終了時点での得点を競います。

## 得点計算
回収した資源の量を、資源の種類ごとに **小さい順に並び替えて** x, y, zとします。
この時に下記の式で計算される値が得点になります。
```
x + 0.1y + 0.01z
```

ゲーム終了時点での得点を競います。ゲーム中の得点は暫定のものとなります。

### 得点計算例
回収した資源の量が 34, 12, 56 とします。
この時の得点は下記の式より 15.96 点となります。
```
12 + 0.1 * 34 + 0.01 * 0.56 = 15.96
```

## 資源回収車

### 移動にかかる時間
資源回収車の移動には時間がかかります。

座標 `(x1, y1)` から `(x2, y2)` に移動する際にかかる時間は以下の式で与えられます。

```
100 * √((x1-x2)² + (y1-y2)²) [ミリ秒]
```

例) `(0, 0)` から `(10, 10)` への移動には 1,414 ミリ秒かかる。

### 即時移動と予約移動
資源回収車の移動には 即時移動と予約移動の2種類が存在します。

- 即時移動
  - その時点の座標(※)からすぐに移動を開始する
  - 予約移動がされているときに即時移動を行なった場合は、その予約移動は無効になる
- 予約移動
  - 指定の時刻になったらその時点の座標(※)から移動を開始する
  - 複数回の予約移動を行なった場合は、最後の予約移動のみが有効になる
  - 過去の時刻を指定した場合は即時移動になる

※ その時点の座標とは
- `(x1, y1)` から `(x2, y2)` に移動中で、その移動にかかる時間が `T` 、移動開始から経過した時間が `t` のとき、`(x1 + (x2-x1)*t/T, y1 + (y2-y1)*t/T)`がその時点の座標となります。

## 資源

## 出現と消滅
- 資源は出現中のみ回収できる
  - 時刻 t1 で出現する資源は t1 以降に回収可能になる
  - 時刻 t2 で消滅する資源は t2 以降に回収不可能になる
- 資源は全参加者共通
  - 出現する資源の時刻や回収量の値は参加中のプレイヤー全員同じ
  - 次に資源が出現する時刻はAPIで取得可能

## 回収量

資源と同じ座標に資源回収車がいる場合に、以下の式に従って 1ms 毎に資源が回収されます。

```
資源ごとに設定された回収量 / 資源と同じ座標にいる全参加者の資源回収車数
```

※ 人気の資源は時間あたりの回収量が減る点に注意

## 資源の出現頻度

- フィールド上に同時に出現している資源の数は 5 以上 15 以下
- フィールド上に出現している資源の数の平均は約 10 個

## 資源の出現位置

- 同じ座標で同時刻に複数の資源が出現することはない
- 資源回収車の初期位置 (0, 0), (0, 30), (15, 15), (30, 0), (30, 30) に資源が出現することはない

## 資源ごとの特徴

- 出現確率
  - 資源A : 50%
  - 資源B : 30%
  - 資源C : 20%
- 出現から消滅までの時間
  - 資源A : 1,000ms 以上 10,000ms 以下
  - 資源B : 5,000ms 以上 10,000ms 以下
  - 資源C : 1,000ms 以上 5,000ms 以下
- 回収速度
  - 資源ごとのパラメータを用いて正規分布に基づく乱数を生成し整数に丸めた値
  - ゲーム開始時のパラメータ
    - 資源A
      - 平均 : √10
      - 標準偏差 : 0.1 * √10
    - 資源B
      - 平均 : 0.5 * √10
      - 標準偏差 : 0.1 * √10
    - 資源C
      - 平均 : 10
      - 標準偏差 : √10
  - この値が 1,000ms ごとの資源の回収量になる
  - 30分ごとに平均・標準偏差ともに √10 倍される
