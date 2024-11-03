{
  description = "Converts OBJ files to OGC 3D tiles by performing splitting, decimation and conversion";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
    flake-parts.url = "github:hercules-ci/flake-parts";
    devenv.url = "github:cachix/devenv";
  };

  outputs = inputs@{ flake-parts, nixpkgs, ... }:
    flake-parts.lib.mkFlake { inherit inputs; } {
      imports = [
        inputs.devenv.flakeModule
      ];
      # systems = nixpkgs.lib.systems.flakeExposed;
      systems = [
        "x86_64-linux"
      ];

      perSystem = { config, self', inputs', pkgs, system, ... }: with pkgs; {
        packages.default = buildDotnetModule {
          name = "Obj2Tiles";
          version = "1.0.12";
          src = ./.;
          nugetDeps = ./deps.nix;
          dotnet-sdk = dotnetCorePackages.dotnet_8.sdk;
          dotnet-runtime = dotnetCorePackages.dotnet_8.runtime;
        };

        devenv.shells.default = {
          languages.dotnet = {
            enable = true;
            package = dotnetCorePackages.dotnet_8.sdk;
          };
        };
      };
    };

}
