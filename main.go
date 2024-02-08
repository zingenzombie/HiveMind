package main

import (
	"encoding/json"
	"fmt"
	"net/http"
)

type HiveServer struct {
	Name         string
	Address      string
	Players      int64
	CoordinatesX int64
	CoordinatesY int64
	CoordinatesZ int64
}

type ServerList struct {
	NumServers  int64
	HiveServers []HiveServer
}

var HiveServers []HiveServer

func hello(w http.ResponseWriter, req *http.Request) {

	fmt.Fprintf(w, "hello\nHow are you?")
}

func servers(w http.ResponseWriter, req *http.Request) {

	m := ServerList{int64(len(HiveServers)), HiveServers}
	b, err := json.Marshal(m)
	if err != nil {
		println("oops")
		return
	}
	w.Write(b)
}

func newServer(w http.ResponseWriter, req *http.Request) {

	var response HiveServer

	err := json.NewDecoder(req.Body).Decode(&response)

	if err != nil {
		http.Error(w, err.Error(), http.StatusBadRequest)

		fmt.Fprintf(w, "Oopsy Woopsy")
		return
	}

	HiveServers = append(HiveServers, response)

	fmt.Fprintf(w, "Loud and clear!!!")

}

func main() {

	http.HandleFunc("/hello", hello)
	http.HandleFunc("/servers", servers)
	http.HandleFunc("/newServer", newServer)
	println("SLAYYYY")
	http.ListenAndServe(":3621", nil)
}
