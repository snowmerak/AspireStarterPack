package main

import (
	"context"
	"errors"
	"log"
	"net/http"
	"os"
	"os/signal"
)

func main() {
	ctx, cancel := signal.NotifyContext(context.Background(), os.Interrupt)
	defer cancel()

	addr := os.Getenv("PORT")
	log.Printf("Server started on %s", addr)

	server := &http.Server{Addr: ":" + addr}

	http.HandleFunc("/", func(writer http.ResponseWriter, request *http.Request) {
		name := request.URL.Query().Get("name")
		writer.Write([]byte("Hello, " + name))
	})

	go func() {
		if err := server.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
			log.Fatalf("Failed to start server: %v", err)
		}
	}()

	<-ctx.Done()
	log.Printf("Shutting down server...")

	if err := server.Shutdown(ctx); err != nil {
		log.Fatalf("Failed to shutdown server: %v", err)
	}
}
