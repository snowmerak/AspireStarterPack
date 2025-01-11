package main

import (
	"os"
	"strings"
)

const (
	replicaSetEnvKey = "ReplicaSet__grpc-echo-server__grpc"
)

func ReadGrpcHosts(envKey string) []string {
	keys := strings.Split(os.Getenv(envKey), ",")
	hosts := make([]string, 0, len(keys))
	for _, key := range keys {
		hosts = append(hosts, strings.TrimPrefix(os.Getenv(key), "tcp://"))
	}

	return hosts
}
