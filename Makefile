all: clean restore build

clean:
	dotnet clean

restore:
	dotnet restore

build:
	dotnet build

