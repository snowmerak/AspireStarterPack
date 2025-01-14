package main

import (
	"context"
	"errors"
	"log"
	"net"
	"net/http"
	"os"
	"os/signal"
	"sync"

	"google.golang.org/grpc"

	"GrpcEchoServer/gen/echo"
)

func main() {
	ctx, cancel := signal.NotifyContext(context.Background(), os.Interrupt)
	defer cancel()

	shutdownCompleted := new(sync.WaitGroup)
	shutdownCompleted.Add(1)
	go func() {

		server := grpc.NewServer()
		echo.RegisterEchoServiceServer(server, &Server{})

		grpcPort := os.Getenv("GRPC_PORT")
		if grpcPort == "" {
			grpcPort = "50051"
		}

		lis, err := net.Listen("tcp", ":"+grpcPort)
		if err != nil {
			panic(err)
		}

		context.AfterFunc(ctx, func() {
			server.GracefulStop()
			shutdownCompleted.Done()
		})

		if err := server.Serve(lis); err != nil && !errors.Is(err, grpc.ErrServerStopped) {
			panic(err)
		}
	}()

	shutdownCompleted.Add(1)
	go func() {
		healthPort := os.Getenv("HEALTH_PORT")
		if healthPort == "" {
			healthPort = "8080"
		}

		httpServer := &http.Server{Addr: ":" + healthPort}
		router := http.NewServeMux()
		router.HandleFunc("/healthz", func(writer http.ResponseWriter, request *http.Request) {
			writer.WriteHeader(http.StatusOK)
		})
		httpServer.Handler = router

		context.AfterFunc(ctx, func() {
			defer shutdownCompleted.Done()

			if err := httpServer.Shutdown(context.TODO()); err != nil {
				log.Printf("Failed to shut down health server: %v", err)
			}
		})

		if err := httpServer.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
			panic(err)
		}
	}()

	shutdownCompleted.Wait()
	log.Printf("server has been shut down")
}

type Server struct {
	echo.UnimplementedEchoServiceServer
}

func (s *Server) Echo(ctx context.Context, request *echo.EchoRequest) (*echo.EchoResponse, error) {
	return &echo.EchoResponse{Message: request.Message}, nil
}

func (s *Server) mustEmbedUnimplementedEchoServiceServer() {}
