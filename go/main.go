/*
実行には go 環境が必要です。
TOKEN 変数を書き換えて実行してください。

サンプル初期実装
移動先に資源が無い回収車を、ランダムに選んだ出現中の資源へと移動させる
ただしこのとき２台以上の回収車が同じ資源を選ばないようにする
*/

package main

import (
	"encoding/json"
	"fmt"
	"io/ioutil"
	"log"
	"math/rand"
	"net/http"
	"os"
	"sort"
	"strings"
	"time"
)

// GameServer ゲームサーバのアドレスとトークン
var GameServer string = "https://contest.2021-autumn.gbc.tenka1.klab.jp"
// var GameServer string = "https://contest.2021-autumn.gbc.tenka1.klab.jp/staging" // 開発用環境
var TOKEN string = "YOUR_TOKEN"

func init() {
	rand.Seed(time.Now().Unix())

	if os.Getenv("GAME_SERVER") != "" {
		GameServer = os.Getenv("GAME_SERVER")
	}
	if os.Getenv("TOKEN") != "" {
		TOKEN = os.Getenv("TOKEN")
	}
}

type AgentMove struct {
	X float64 `json:"x"`
	Y float64 `json:"y"`
	T int     `json:"t"`
}

type Agent struct {
	Move []AgentMove `json:"move"`
}

type Resource struct {
	ID     int    `json:"id"`
	X      int    `json:"x"`
	Y      int    `json:"y"`
	T0     int    `json:"t0"`
	T1     int    `json:"t1"`
	Type   string `json:"type"`
	Weight int    `json:"weight"`
}

type ResourceWithAmount struct {
	Resource
	Amount float64 `json:"amount"`
}

type OwnedResource struct {
	Type   string  `json:"type"`
	Amount float64 `json:"amount"`
}

type Game struct {
	Status        string          `json:"status"`
	Now           int             `json:"now"`
	Agent         []Agent         `json:"agent"`
	Resource      []Resource      `json:"resource"`
	NextResource  int             `json:"next_resource"`
	OwnedResource []OwnedResource `json:"owned_resource"`
}

type Move struct {
	Status string      `json:"status"`
	Now    int         `json:"now"`
	Move   []AgentMove `json:"move"`
}

type Resources struct {
	Status   string               `json:"status"`
	Resource []ResourceWithAmount `json:"resource"`
}

func callAPI(x string) ([]byte, error) {
	url := GameServer + x
	fmt.Println(url)
	resp, err := http.Get(url)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()
	body, err := ioutil.ReadAll(resp.Body)
	if resp.StatusCode != 200 {
		return nil, fmt.Errorf(resp.Status)
	}
	return body, err
}

func callGame() (*Game, error) {
	res, err := callAPI(fmt.Sprintf("/api/game/%s", TOKEN))
	if err != nil {
		return nil, err
	}
	var game Game
	err = json.Unmarshal(res, &game)
	return &game, err
}

func callMove(index int, x int, y int) (*Move, error) {
	res, err := callAPI(fmt.Sprintf("/api/move/%s/%d-%d-%d", TOKEN, index, x, y))
	if err != nil {
		return nil, err
	}
	var move Move
	err = json.Unmarshal(res, &move)
	return &move, err
}

func callWillMove(index int, x int, y int, t int) (*Move, error) {
	res, err := callAPI(fmt.Sprintf("/api/will_move/%s/%d-%d-%d-%d", TOKEN, index, x, y, t))
	if err != nil {
		return nil, err
	}
	var move Move
	err = json.Unmarshal(res, &move)
	return &move, err
}

func callResources(ids []int) (*Resources, error) {
	var strIds []string
	for _, id := range ids {
		strIds = append(strIds, fmt.Sprint(id))
	}
	res, err := callAPI(fmt.Sprintf("/api/resources/%s/%s", TOKEN, strings.Join(strIds, "-")))
	if err != nil {
		return nil, err
	}
	var resources Resources
	err = json.Unmarshal(res, &resources)
	return &resources, err
}

func calcScore(game *Game) float64 {
	a := []float64{}
	for _, o := range game.OwnedResource {
		a = append(a, o.Amount)
	}
	sort.Float64s(a)
	return a[0] + 0.1 * a[1] + 0.01 * a[2]
}

type Bot struct {
}

func NewBot() *Bot {
	return &Bot{}
}

func (bot *Bot) solve() {
	for {
		game, err := callGame()
		if err != nil {
			log.Fatal(err)
		}
		if game.Status != "ok" {
			log.Fatal(game.Status)
		}

		for _, o := range game.OwnedResource {
			fmt.Printf("%s: %.2f ", o.Type, o.Amount)
		}
		fmt.Printf("Score: %.2f\n", calcScore(game))

		type Point struct {
			X int
			Y int
		}
		resourcePositions := map[Point]bool{}
		for _, r := range game.Resource {
			if r.T0 <= game.Now && game.Now < r.T1 {
				resourcePositions[Point{X: r.X, Y: r.Y}] = true
			}
		}

		var indexList []int
		for index := 1; index <= 5; index++ {
			agentMove := game.Agent[index-1].Move
			m := agentMove[len(agentMove)-1]
			p := Point{X: int(m.X), Y: int(m.Y)}

			if resourcePositions[p] {
				delete(resourcePositions, p)
			} else {
				indexList = append(indexList, index)
			}
		}

		for _, index := range indexList {
			if len(resourcePositions) == 0 {
				break
			}

			var a []Point
			for p := range resourcePositions {
				a = append(a, p)
			}

			p := a[rand.Intn(len(a))]
			_, err = callMove(index, p.X, p.Y)
			if err != nil {
				log.Fatal(err)
			}
			delete(resourcePositions, p)
		}

		time.Sleep(1000 * time.Millisecond)
	}
}

func main() {
	bot := NewBot()
	bot.solve()
}
