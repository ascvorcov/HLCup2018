rd /S /Q .\out
dotnet publish -r win7-x64 -f netcoreapp2.2 -c Release --output .\out
start .\out\hlcup2018.exe input_min
