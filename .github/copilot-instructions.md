# AGENTS.md

## Purpose

This repository contains **FluxFX**, a GPU-driven particle/VFX package
for Unity 2022.3 / VRChat Worlds.

FluxFX is built on top of **Flux**, a small low-level GPGPU foundation
for environments where ComputeShader is unavailable at runtime. In
VRChat/Udon, GPU work is primarily performed with RenderTextures,
shaders, `VRCGraphics.Blit`, and `VRCAsyncGPUReadback`.

When modifying this repository, preserve the architectural boundaries
and development principles below. Do not introduce abstractions merely
because they may be useful later.

------------------------------------------------------------------------

## Target Environment

-   Unity 2022.3.22f1
-   Built-in Render Pipeline
-   VRChat Worlds
-   UdonSharp
-   Quest must remain an important target
-   ComputeShader is not available to FluxFX runtime code
-   FluxFX package ID: `com.tplab.flux.fx`
-   Flux Core package ID: `com.tplab.flux`

Namespaces:

-   Runtime Udon: `TpLab.Flux.FX.Udon`
-   Runtime normal C#/MonoBehaviour authoring: `TpLab.Flux.FX.Scripts`
-   Editor: `TpLab.Flux.FX.Editor`
-   Tests: `TpLab.Flux.FX.Tests.Udon`

Flux Core Udon namespace:

-   `TpLab.Flux.Udon`

------------------------------------------------------------------------

## Architectural Principles

### Flux Core stays small

Flux Core is the low-level GPGPU foundation.

Its primary responsibilities are:

-   `FluxBuffer`: GPU storage
-   `FluxKernel`: primitive single-pass GPU operation
-   `FluxUpload`: CPU -\> GPU boundary
-   `FluxReadback`: GPU -\> CPU asynchronous boundary
-   `FluxReduction`: composite many-to-one GPU reduction
-   shared low-level shader addressing/metadata

Do **not** move FluxFX-specific concepts into Flux Core.

Examples of concepts that belong in FluxFX rather than Core:

-   particles
-   emitters
-   particle modules
-   particle simulation space
-   particle render modes
-   particle attributes
-   particle curves/gradients
-   particle spawn behavior

Do not add generic Core abstractions such as Binder, Pipeline, PingPong,
Attribute Layout, or similar systems until multiple real upper packages
demonstrate a concrete shared requirement.

### GPU data stays on the GPU

The normal data flow is GPU -\> GPU.

CPU -\> GPU uploads and GPU -\> CPU readbacks are boundary operations,
not the normal simulation path.

Avoid designs that require reading particle state back to the CPU every
frame.

### Logical modules are not physical GPU passes

Keep these concepts separate:

`Logical Module -> Execution Stage -> Physical GPU Pass`

A user-facing feature does not automatically require its own
RenderTexture or Blit.

Likewise, multiple logical modules may eventually be compiled into one
physical shader/pass.

Current broad execution stages are:

-   Spawn
-   Velocity
-   Position
-   Render

Do not create one GPU pass per Inspector module by default.

### Prefer fixed feature shaders for now

FluxFX currently uses fixed feature shaders with compiled parameters.

Do not introduce a generated-shader system yet.

Shader generation may become appropriate after the module set, particle
data model, and physical pass requirements are sufficiently stable.

### Package-first, API-later

Develop real package functionality first.

Public APIs should emerge from concrete usage and tests rather than
speculative framework design.

Avoid large generalized APIs created only for hypothetical future
features.

------------------------------------------------------------------------

## FluxFX Product Direction

FluxFX aims for a **Unity ParticleSystem-like authoring experience**,
implemented using Flux-compatible GPU techniques for VRChat.

It does not need to reproduce every Unity ParticleSystem feature
exactly.

Expected major areas include:

-   Main
-   Emission
-   Shape
-   Initial Velocity
-   Velocity over Lifetime
-   Force / Gravity
-   Drag
-   Noise
-   Vortex
-   Color over Lifetime
-   Size over Lifetime
-   Rotation / Rotation over Lifetime
-   Renderer
-   Local / World Simulation Space

Possible future areas include:

-   Texture Sheet Animation
-   Custom Data
-   Trails
-   Collision
-   Triggers
-   Sub Emitters
-   FluxField integration

Do not implement future areas prematurely.

------------------------------------------------------------------------

## Particle Data Model

Keep **logical particle data** separate from its **physical GPU
packing**.

Conceptually particle-related values fall into the following categories.

### Authoring Parameters

Definitions configured by the user.

Examples:

-   Start Lifetime
-   Start Speed
-   Start Size
-   Start Rotation
-   Start Color
-   Gravity
-   Drag
-   Noise
-   Color over Lifetime
-   Size over Lifetime
-   Rotation over Lifetime

These are compiled into runtime parameters and, in the future where
appropriate, LUT textures.

### Dynamic State

Values that change as simulation advances.

Examples:

-   Position
-   Velocity
-   Age
-   future Orientation or Angular Velocity only if simulation actually
    requires persistent state

### Spawn Attributes

Values evaluated when a particle is spawned and normally retained for
that particle's lifetime.

Examples:

-   Lifetime
-   Start Color
-   Start Size
-   Start Rotation
-   other randomized start values

A Spawn Attribute is a **logical concept**. It does not imply that every
attribute needs a dedicated RenderTexture.

### Derived Values

Values that can be calculated from state, spawn attributes, and compiled
parameters rather than stored as persistent state.

Examples:

-   Normalized Age
-   Current Color
-   Current Size
-   Current Rotation

Prefer derived values over additional persistent state when practical.

### Physical packing may differ from logical meaning

Current/future GPU packing may place unrelated logical values in the
same `float4`.

That is acceptable.

Do not force the GPU buffer layout to mirror the logical object model.

Conversely, avoid anonymous `Attribute0`, `Attribute1`, etc.
abstractions until a real dynamic layout/compiler exists and provides a
concrete benefit.

------------------------------------------------------------------------

## Value System Direction

The long-term authoring model should support Unity ParticleSystem-like
value definitions.

Typical numeric/vector modes:

-   Constant
-   Random Between Two Constants
-   Curve
-   Random Between Two Curves

Colors should have equivalent constant/random/gradient forms where
appropriate.

The conceptual flow is:

`Authoring Value Definition -> Compile -> Spawn/Lifetime Evaluation -> Particle Attribute or Derived Value`

Spawn-time values should generally be evaluated once when the particle
is spawned.

Over-lifetime values should generally be evaluated from normalized age.

Curves and gradients are expected to compile to GPU-friendly
representations such as LUT textures when that system is implemented.

Do not build a large generic Value framework before it is needed by
concrete modules.

------------------------------------------------------------------------

## Randomness

Do not make random seed the central particle abstraction.

Randomness is an implementation detail used by value evaluation when a
configured mode requires random variation.

A future deterministic random source may use a particle/spawn identifier
or seed, but user-facing modules should be expressed primarily in terms
of their value definitions.

Do not reconstruct every spawn attribute from a seed every frame when
storing the evaluated spawn attribute is the clearer model.

------------------------------------------------------------------------

## Simulation Space

FluxFX supports:

-   Local
-   World

The **FluxParticleSystem root Transform** is the simulation/emission
reference Transform.

The renderer child is an internal rendering object and should not define
the emitter coordinate system.

### Local

Particle Position and Velocity are stored in system-local simulation
space.

Changing the FluxParticleSystem root Transform after spawn moves/rotates
the existing particles with the system.

Rendering converts local particle positions through the object
transform.

### World

At spawn time:

-   Shape-local position is transformed using the current system
    Transform and stored as world position.
-   Initial velocity direction is transformed using the current system
    rotation and stored as world velocity.
-   System scale affects spawn/shape position.
-   System scale does not multiply the initial velocity magnitude.

After spawn, existing particles remain independent of later system
Transform changes.

New particles use the current system Transform at their spawn time.

Do not accidentally apply the renderer child's Transform as an
additional simulation transform.

------------------------------------------------------------------------

## Renderer Architecture

`FluxParticleRenderer` is expected to live on a child GameObject beneath
the particle system root.

Do not assume it is on the same GameObject as `FluxParticleSystem`.

Editor/build code should locate it with child lookup where appropriate.

The renderer does **not** currently use GPU Instancing.

It builds one combined Mesh:

`Source Mesh x Particle Capacity`

Each replicated source vertex carries its particle index, currently
through UV2, and the vertex shader samples GPU particle state.

Geometry is generated on the CPU only during initialization; particle
movement remains GPU-driven.

Mesh mode should favor low-poly source meshes, especially for Quest.

Source Mesh submesh/material structure is intentionally not inherited.
Mesh Render Mode renders using the single Material configured on the
FluxFX Renderer.

------------------------------------------------------------------------

## Shader Rules

Flux Core common include:

``` hlsl
#include "Packages/com.tplab.flux/Runtime/Shaders/FluxCommon.hlsl"
```

RenderTextures containing FluxBuffer `ARGBFloat` data should use
`sampler2D_float`, including additional particle buffers.

Keep shared particle-specific shader helpers in FluxFX rather than Flux
Core unless they become genuinely package-independent.

Be careful with Quest compatibility.

Do not assume desktop-only shader behavior is acceptable.

------------------------------------------------------------------------

## Current Runtime Direction

FluxFX currently uses separate GPU state for particle simulation,
including Position, Velocity, and visual/spawn-related data.

Current established concepts include:

-   GPU particle state
-   ping-pong simulation where required by Blit-based updates
-   round-robin emission/spawn slots
-   GPU respawn
-   lifetime/age
-   Point and Sphere shapes
-   initial velocity
-   gravity
-   drag
-   noise
-   vortex
-   start color
-   start size
-   color over lifetime
-   size over lifetime
-   Billboard and Mesh render modes
-   fixed start rotation groundwork
-   Local and World Simulation Space
-   compiled JSON parameters
-   editor authoring separated from Udon runtime

Do not rewrite these systems into a new architecture without a concrete
reason.

------------------------------------------------------------------------

## Compiled Parameters

FluxFX authoring is compiled into JSON by the Editor compiler.

Runtime Udon deserializes compiled parameters using VRChat data APIs.

Treat this JSON as a compiled parameter representation / intermediate
representation, not as the authoring model itself.

General rule:

-   deserialize once during initialization
-   configure stable parameters during initialization
-   keep dynamic hot-path values as direct runtime operations

Enabled modules may be represented sparsely: absence can mean disabled.

The compiler may eventually also drive generated shader variants, but do
not implement that until requirements stabilize.

------------------------------------------------------------------------

## UdonSharp Constraints

Remember that normal C# features are not automatically safe in
UdonSharp.

In particular:

-   Do not use object initializer syntax in UdonSharp runtime code.
-   Do not assume arbitrary Unity/.NET APIs are supported by Udon.
-   Avoid unnecessary allocations in runtime hot paths.
-   Required serialized references do not need defensive null checks
    unless absence is a supported state.
-   Verify Udon/VRChat compatibility before introducing new runtime
    APIs.

Editor code and normal MonoBehaviour authoring code are not subject to
all UdonSharp restrictions.

------------------------------------------------------------------------

## C# Style

Follow these rules consistently.

### Fields

Serialized fields:

``` csharp
[SerializeField]
float speed;
```

-   Attribute on its own line.
-   Serialized field names do not use a leading underscore.

Non-serialized private/runtime fields use a leading underscore:

``` csharp
float _time;
```

Explicit `private` is normally omitted.

### Locals

Prefer `var` where the type is clear.

### Control flow

A one-line early return may omit braces:

``` csharp
if (!enabled) return;
```

Other `if` statements must use braces:

``` csharp
if (enabled)
{
    Apply();
}
```

All loops must use braces, even for one statement:

``` csharp
for (var i = 0; i < count; i++)
{
    UpdateParticle(i);
}
```

This also applies to `continue` and `break` cases:

``` csharp
if (!valid)
{
    continue;
}
```

Do not write:

``` csharp
if (!valid) continue;
```

### Class ordering

Prefer:

1.  Constants
2.  Serialized fields
3.  Private/runtime fields
4.  Public properties
5.  Public methods / Public API
6.  Unity Events
7.  Udon Events
8.  Private methods

Do not use `#region` merely to separate Public API.

### Public API

Use:

``` csharp
using JetBrains.Annotations;
```

and `[PublicAPI]` for members intentionally exposed as package public
API.

Do not mark a member `[PublicAPI]` merely because it must technically be
`public` for an internal mechanism.

### Change scope

Preserve existing structure where possible.

Prefer the smallest coherent change that implements the agreed design.

Do not perform unrelated cleanup or architectural refactors while
implementing a feature unless explicitly requested.

------------------------------------------------------------------------

## Working Method for AI Agents

Before editing:

1.  Inspect the actual current implementation.
2.  Trace relevant call sites and data flow.
3.  Preserve already validated behavior.
4.  Distinguish known facts from hypotheses.
5.  Choose the smallest coherent implementation.

Do not guess the hierarchy, current APIs, or shader data layout.

Do not make speculative changes based on what the code "probably"
contains.

When a behavior has already been validated as PASS, treat it as a
regression boundary.

For risky architectural changes, explain the proposed boundary before
rewriting large areas.

When requirements are ambiguous, prefer the existing architecture and
minimal change over inventing a generalized framework.

------------------------------------------------------------------------

## Validation Priorities

For each feature, verify both desktop behavior and VRChat/Udon
constraints where relevant.

Important regression areas include:

-   particle count/capacity behavior
-   emission slot reuse
-   age/lifetime
-   respawn
-   Position/Velocity ping-pong
-   visual/spawn attribute preservation
-   Mesh mode
-   Quest-compatible float texture sampling
-   Local Simulation Space transform behavior
-   World Simulation Space independence after spawn

Do not treat successful C# compilation alone as sufficient validation
for shader/Udon behavior.

------------------------------------------------------------------------

## Long-Term Flux Ecosystem

FluxFX is one upper package in a larger Flux ecosystem.

Expected packages include:

-   FluxFX --- particles/VFX
-   FluxSwarm --- boids/group behavior
-   FluxField --- scalar/vector/noise/flow/distance GPU fields
-   FluxSurface --- wave/ripple/deformation
-   FluxInstance --- large-scale GPU instance-oriented rendering
-   FluxGrid --- cellular automata/reaction diffusion

FluxField may eventually become important compositional glue between
upper packages.

Do not preemptively force FluxFX abstractions into these other packages.

Shared abstractions should move downward only after concrete reuse is
demonstrated.

------------------------------------------------------------------------

## Decision Rule

When choosing between:

-   a small implementation that satisfies the current feature and
    preserves architectural room, and
-   a generalized framework for hypothetical future needs,

prefer the small implementation.

When choosing between:

-   CPU-driven convenience, and
-   keeping simulation data GPU-resident,

prefer the GPU-resident design unless there is a concrete reason not to.

When choosing between:

-   matching Unity ParticleSystem's exact internals, and
-   providing a familiar ParticleSystem-like authoring experience that
    fits Flux/VRChat constraints,

prefer the latter.
