#include <stdlib.h>
#include <iostream>
#include <string>
#include <vector>
#include <set>
#include <thread>
#include <chrono>
#include <queue>
#include <random>
#include <algorithm>
#include <map>
using namespace std;

using Vertex = pair<int,int>;

mt19937 mt;

struct AgentMove {
	double x, y;
	int t;
};

struct Agent {
	vector<AgentMove> move;
};

struct Resource {
	int id, x, y, t0, t1;
	string type;
	int weight;
};

struct ResourceWithAmount : public Resource {
	double amount;
};

struct OwnedResource {
	string type;
	double amount;
};

struct Game {
	int now;
	vector<Agent> agent;
	vector<Resource> resource;
	int next_resource;
	vector<OwnedResource> owned_resource;

	map<Vertex, Resource> resource_by_vertex;

	inline Resource find_resource_by_vertex(const Vertex& p) const {
		return resource_by_vertex.at(p);
	}

	inline Vertex get_agent_position(int index) const {
		auto&& p = agent.at(index).move.back();
		return {p.x, p.y};
	}
};

struct Move {
	int now;
	vector<AgentMove> move;
};

struct Resources {
	vector<ResourceWithAmount> resource;
};

Game call_game() {
	cout << "game" << endl;
	Game res;
	int num_agent, num_resource, num_owned_resource;
	cin >> res.now >> num_agent >> num_resource >> res.next_resource >> num_owned_resource;
	res.agent.resize(num_agent);
	for (auto& a : res.agent) {
		int num_move;
		cin >> num_move;
		a.move.resize(num_move);
		for (auto& m : a.move) {
			cin >> m.x >> m.y >> m.t;
		}
	}
	res.resource.resize(num_resource);
	for (auto& r : res.resource) {
		cin >> r.id >> r.x >> r.y >> r.t0 >> r.t1 >> r.type >> r.weight;
		res.resource_by_vertex.emplace(make_pair(r.x, r.y), r);
	}
	res.owned_resource.resize(num_owned_resource);
	for (auto& o : res.owned_resource) {
		cin >> o.type >> o.amount;
	}
	return res;
}

Move read_move() {
	Move res;
	int num_move;
	cin >> res.now >> num_move;
	res.move.resize(num_move);
	for (auto& m : res.move) {
		cin >> m.x >> m.y >> m.t;
	}
	return res;
}

Move call_move(int index, int x, int y) {
	cout << "move " << index << " " << x << " " << y << endl;
	return read_move();
}

Move call_will_move(int index, int x, int y, int t) {
	cout << "will_move " << index << " " << x << " " << y << " " << t << endl;
	return read_move();
}

Resources call_resources(vector<int> ids) {
	cout << "resources";
	for (auto id : ids) {
		cout << " " << id;
	}
	cout << endl;
	Resources res;
	int num_resource;
	cin >> num_resource;
	res.resource.resize(num_resource);
	for (auto& r : res.resource) {
		cin >> r.id >> r.x >> r.y >> r.t0 >> r.t1 >> r.type >> r.weight >> r.amount;
	}
	return res;
}

double calc_score(const Game& game) {
	vector<double> a;
	for (const auto& o : game.owned_resource) {
		a.push_back(o.amount);
	}
	sort(a.begin(), a.end());
	return a[0] + 0.1 * a[1] + 0.01 * a[2];
}

struct Bot {
	Game game;

	inline int expect_earned_score(Vertex agent_position, Resource resource) {
		int r = uniform_int_distribution<>(0, 10)(mt);
		return r;
	}

	Vertex select_resource(int agent_index, const set<Vertex>& resource_positions) {
		Vertex selected;
		int score = -1;
		for (auto&& p : resource_positions) {
			auto&& r = game.find_resource_by_vertex(p);
			int expected_score = expect_earned_score(game.get_agent_position(agent_index), r);
			cerr << r.id << "\t(" << r.x << "," << r.y << ")\t" << expected_score << "\n";
			if (expected_score > score) {
				score = expected_score;
				selected = p;
			}
		}
		return selected;
	}

	void solve() {
		for (;;) {
			game = call_game();

			for (const auto& o : game.owned_resource) {
				fprintf(stderr, "%s: %.2f ", o.type.c_str(), o.amount);
			}
			fprintf(stderr, "Score: %.2f\n", calc_score(game));

			set<Vertex> resource_positions;
			for (const auto& r : game.resource) {
				if (r.t0 <= game.now && game.now < r.t1) {
					resource_positions.insert({r.x, r.y});
				}
			}

			vector<int> moving_agent_indices;
			for (int i = 0; i < 5; ++ i) {
				const auto& m = game.agent[i].move.back();
				if (resource_positions.count({m.x, m.y})) {
					resource_positions.erase({m.x, m.y});
				} else {
					moving_agent_indices.push_back(i);
				}
			}

			for (int index : moving_agent_indices) {
				if (resource_positions.empty()) break;
				auto&& p = select_resource(index, resource_positions);
				cerr << index+1 << " " << p.first << " " << p.second << "\n";
				call_move(index+1, p.first, p.second);
				resource_positions.erase(p);
			}

			this_thread::sleep_for(chrono::milliseconds(1000));
		}
	}
};

int main() {
	random_device seed_gen;
	mt = mt19937(seed_gen());

	Bot bot;
	bot.solve();
}
