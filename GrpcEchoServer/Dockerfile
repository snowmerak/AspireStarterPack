FROM docker.io/library/golang:1.23.4 as Builder

WORKDIR /build

COPY go.mod go.sum ./

RUN go mod download

COPY . .

RUN CGO_ENABLED=0 go build -o server .

FROM docker.io/library/alpine:3.21

WORKDIR /app

COPY --from=Builder /build/server .

CMD ["./server"]
