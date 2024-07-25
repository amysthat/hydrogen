set -e

echo "Building solution..."
dotnet build --verbosity quiet

echo
echo "Running tests..."
dotnet test --verbosity quiet

echo
echo "Compiling 'test.hy'..."
Hydrogen/bin/Debug/net8.0/linux-x64/Hydrogen user/test.hy /optimizepushpull /exporttokens
