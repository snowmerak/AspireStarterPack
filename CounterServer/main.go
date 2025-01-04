package main

import (
	"bytes"
	"context"
	"log"
	"net/http"
	"os"
	"strconv"
	"sync"

	"github.com/google/uuid"
	"github.com/valkey-io/valkey-go"
)

func main() {
	generatedUUID, err := uuid.NewUUID()
	if err != nil {
		log.Fatalf("Failed to generate UUID: %v", err)
	}
	id := generatedUUID.String()

	cacheClient, err := valkey.NewClient(valkey.ClientOption{
		InitAddress: []string{"SharedCache:6379"},
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

	http.ListenAndServe(":"+addr, nil)
}
