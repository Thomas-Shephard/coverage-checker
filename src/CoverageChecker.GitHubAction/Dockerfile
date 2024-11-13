ARG DOTNET_VERSION

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build

ARG DOTNET_VERSION

# Copy the source code to /app
WORKDIR /app
COPY . ./

# Run dotnet publish to produce the executable
RUN dotnet publish src/CoverageChecker.GitHubAction/CoverageChecker.GitHubAction.csproj \
    --configuration  Release \
    --framework net${DOTNET_VERSION} \
    --output out \
    --self-contained false

# Label the container with the GitHub repository
LABEL repository="https://github.com/Thomas-Shephard/coverage-checker"
LABEL homepage="https://github.com/Thomas-Shephard/coverage-checker"
# Label the container as a GitHub action
LABEL com.github.actions.name="Coverage Checker"
LABEL com.github.actions.description="Checks the code coverage of a project"

FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION} AS runtime

# Copy the published application to the runtime image
COPY --from=build /app/out .

# Run the GitHubAction on entry
ENTRYPOINT ["dotnet", "/CoverageChecker.GitHubAction.dll"]