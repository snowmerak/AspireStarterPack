package main

import (
	"log"
	"net/http"
	"os"
)

func main() {
	addr := os.Getenv("PORT")
	log.Printf("Server started on %s", addr)

	http.HandleFunc("/", func(writer http.ResponseWriter, request *http.Request) {
		name := request.URL.Query().Get("name")
		writer.Write([]byte("Hello, " + name))
	})

	http.ListenAndServe(":"+addr, nil)
}
