# Build C++ WebRTC library
cd native
mkdir build && cd build
cmake .. -DWEBRTC_ROOT=/path/to/webrtc
cmake --build . --config Release

# Build C# desktop app
cd ../../RunApp.Desktop
dotnet add package Microsoft.AspNetCore.SignalR.Client
dotnet build

# Run
dotnet run