@echo off
setlocal

set ROOT=Tutorial01B

echo Creating project structure...

rem Root folder
if not exist "%ROOT%" mkdir "%ROOT%"

rem Subfolders
if not exist "%ROOT%\Models" mkdir "%ROOT%\Models"
if not exist "%ROOT%\Extensions" mkdir "%ROOT%\Extensions"
if not exist "%ROOT%\Clients" mkdir "%ROOT%\Clients"
if not exist "%ROOT%\Services" mkdir "%ROOT%\Services"
if not exist "%ROOT%\Tests" mkdir "%ROOT%\Tests"

rem Root files
if not exist "%ROOT%\SampleOpenAIApp.csproj" type nul > "%ROOT%\SampleOpenAIApp.csproj"
if not exist "%ROOT%\Program.cs" type nul > "%ROOT%\Program.cs"
if not exist "%ROOT%\appsettings.json" type nul > "%ROOT%\appsettings.json"

rem Models
if not exist "%ROOT%\Models\OpenAISettings.cs" type nul > "%ROOT%\Models\OpenAISettings.cs"

rem Extensions
if not exist "%ROOT%\Extensions\ServiceCollectionExtensions.cs" type nul > "%ROOT%\Extensions\ServiceCollectionExtensions.cs"

rem Clients
if not exist "%ROOT%\Clients\IChatCompletionExecutor.cs" type nul > "%ROOT%\Clients\IChatCompletionExecutor.cs"
if not exist "%ROOT%\Clients\OpenAIChatCompletionExecutor.cs" type nul > "%ROOT%\Clients\OpenAIChatCompletionExecutor.cs"

rem Services
if not exist "%ROOT%\Services\IChatService.cs" type nul > "%ROOT%\Services\IChatService.cs"
if not exist "%ROOT%\Services\ChatService.cs" type nul > "%ROOT%\Services\ChatService.cs"

rem Tests
if not exist "%ROOT%\Tests\SampleOpenAIApp.Tests.csproj" type nul > "%ROOT%\Tests\SampleOpenAIApp.Tests.csproj"
if not exist "%ROOT%\Tests\ChatServiceTests.cs" type nul > "%ROOT%\Tests\ChatServiceTests.cs"

echo Done.
echo.
tree "%ROOT%" /f

endlocal
pause