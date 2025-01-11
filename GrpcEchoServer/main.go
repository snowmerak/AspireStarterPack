package main

import (
	"context"
	"net"
	"os"

	"google.golang.org/grpc"

	"GrpcEchoServer/gen/echo"
)

func main() {
	server := grpc.NewServer()
	echo.RegisterEchoServiceServer(server, &Server{})

	port := os.Getenv("PORT")
	if port == "" {
		port = "50051"
	}

	lis, err := net.Listen("tcp", ":"+port)
	if err != nil {
		panic(err)
	}

	if err := server.Serve(lis); err != nil {
		panic(err)
	}
}

type Server struct {
	echo.UnimplementedEchoServiceServer
}

func (s *Server) Echo(ctx context.Context, request *echo.EchoRequest) (*echo.EchoResponse, error) {
	return &echo.EchoResponse{Message: request.Message}, nil
}

func (s *Server) mustEmbedUnimplementedEchoServiceServer() {}
