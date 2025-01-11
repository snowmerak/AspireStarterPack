package main

import (
	"context"
	"errors"
	"log"
	"math/rand"
	"net/http"
	"os"
	"os/signal"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"

	"GrpcEchoServer/gen/echo"
)

func main() {
	ctx, cancel := signal.NotifyContext(context.Background(), os.Interrupt)
	defer cancel()

	addr := os.Getenv("PORT")
	log.Printf("Server started on %s", addr)

	server := &http.Server{Addr: ":" + addr}

	grpcClientHosts := ReadGrpcHosts(replicaSetEnvKey)
	log.Printf("Grpc client hosts: %v", grpcClientHosts)

	http.HandleFunc("/", func(writer http.ResponseWriter, request *http.Request) {
		name := request.URL.Query().Get("name")

		cli, err := grpc.NewClient(grpcClientHosts[rand.Intn(len(grpcClientHosts))], grpc.WithTransportCredentials(insecure.NewCredentials()))
		if err != nil {
			writer.Write([]byte(err.Error()))
			writer.WriteHeader(http.StatusInternalServerError)
			return
		}
		defer cli.Close()

		echoCli := echo.NewEchoServiceClient(cli)

		resp, err := echoCli.Echo(context.Background(), &echo.EchoRequest{Message: "Hello, " + name + "!"})
		if err != nil {
			writer.Write([]byte(err.Error()))
			writer.WriteHeader(http.StatusInternalServerError)
			return
		}

		writer.Write([]byte(resp.GetMessage()))
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
