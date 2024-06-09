ARG DOTNET_VERSION=8.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} as build

WORKDIR /app
COPY . ./
RUN dotnet publish ./CoverageChecker.GitHubAction/CoverageChecker.GitHubAction.csproj -c Release -o out --no-self-contained

LABEL repository="https://github.com/Thomas-Shephard/coverage-checker"
LABEL homepage="https://github.com/Thomas-Shephard/coverage-checker"

LABEL com.github.actions.name="Coverage Checker"
LABEL com.github.actions.description="Checks the code coverage of a project"

FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION} as runtime

COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "/CoverageChecker.GitHubAction.dll"]