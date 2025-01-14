package main

import (
	"context"
	"log"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"

	"GrpcEchoServer/gen/echo"
)

const remoteHost = "localhost:34915"

func main() {
	cli, err := grpc.NewClient(remoteHost, grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		panic(err)
	}
	defer cli.Close()

	echoCli := echo.NewEchoServiceClient(cli)

	resp, err := echoCli.Echo(context.Background(), &echo.EchoRequest{Message: "Hello, world!"})
	if err != nil {
		panic(err)
	}

	log.Println(resp.GetMessage())
}
