# FluxFX

FluxFX is a GPU particle and effects system for Unity, built on top of Flux.

It provides a reusable foundation for creating and controlling GPU-driven particle effects while keeping simulation and rendering extensible.

> [!WARNING]
> FluxFX is currently in early development.
> APIs and package structure may change without notice.

## Features

- GPU-based particle simulation
- GPU-based particle rendering
- Customizable particle data
- Extensible simulation processing
- Flux integration

## Requirements

- Unity 2022.3 or later
- Flux

## Installation

FluxFX can be installed through the Unity Package Manager.

Open **Package Manager > Add package from git URL...** and enter the Git repository URL:

```text
https://github.com/<OWNER>/<REPOSITORY>.git
```

FluxFX requires Flux to be installed in the project.

## About Flux

FluxFX is built on [Flux](<FLUX_REPOSITORY_URL>), a lightweight GPGPU framework for Unity.

Flux provides the low-level GPU processing foundation, while FluxFX provides higher-level particle and effects functionality.

## Status

FluxFX is currently in early development and intended for testing and evaluation.

Breaking changes may occur in future releases.

## License

See the `LICENSE` file for details.