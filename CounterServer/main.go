package main

import (
	"bytes"
	"context"
	"errors"
	"log"
	"net/http"
	"os"
	"os/signal"
	"strconv"
	"strings"
	"sync"

	"github.com/google/uuid"
	"github.com/valkey-io/valkey-go"
)

func main() {
	ctx, cancel := signal.NotifyContext(context.Background(), os.Interrupt)
	defer cancel()

	generatedUUID, err := uuid.NewUUID()
	if err != nil {
		log.Fatalf("Failed to generate UUID: %v", err)
	}
	id := generatedUUID.String()

	cacheHosts := os.Getenv("distributed-cache__nodes")

	cacheClient, err := valkey.NewClient(valkey.ClientOption{
		InitAddress: strings.Split(cacheHosts, ","),
	})
	if err != nil {
		log.Fatalf("Failed to create cache client: %v", err)
	}

	addr := os.Getenv("PORT")
	log.Printf("Server started on %s", addr)

	bufferPool := sync.Pool{
		New: func() interface{} {
			return new(bytes.Buffer)
		},
	}

	http.HandleFunc("/healthz", func(writer http.ResponseWriter, request *http.Request) {
		writer.WriteHeader(http.StatusOK)
	})

	http.HandleFunc("/", func(writer http.ResponseWriter, request *http.Request) {
		v, err := cacheClient.Do(request.Context(), cacheClient.B().Incr().Key("counter").Build()).AsInt64()
		if err != nil {
			log.Printf("Failed to increment counter: %v", err)
			writer.WriteHeader(http.StatusInternalServerError)
			return
		}

		buffer := bufferPool.Get().(*bytes.Buffer)
		context.AfterFunc(request.Context(), func() {
			buffer.Reset()
			bufferPool.Put(buffer)
		})

		buffer.WriteString(id)
		buffer.WriteString(": ")
		buffer.WriteString(strconv.FormatInt(v, 10))

		writer.Write(buffer.Bytes())
	})

	server := &http.Server{Addr: ":" + addr}

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
