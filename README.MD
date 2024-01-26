# Praxis Game Engine

A C# ECS-based game engine.

Under heavy construction, check back later!

## Goals

The primary governing goal of Praxis is that *it must be kept minimal*. In particular:

- Graphics ought to be very simple - basic forward rendering and a simple post FX stack. No PBR or fancy lighting here - there's at least three other engines with bigger communities and better docs you can use if you want that.
- The engine and the tools should be kept 100% separate. The engine doesn't really care where its content files come from, and the tools do not care what engine is consuming the files they produce. I *do not want* a big heavy monolithic editor. Case in point: Open World Builder will be the canonical level editor.
- The engine relies on several third party libraries, and should generally not attempt to hide or abstract that from you. The engine is built on FNA, and so the whole FNA API is available to you.

In short: Praxis can't compete with Godot, Unity, or Unreal, so it *shouldn't try to*. It's designed for specific use cases, and more advanced cases are better left to those other engines.

## Why ECS?

Praxis is an ECS game engine. This means your games are made of worlds, each of which has a set of entities, lightweight components on those entities, and systems which operate on entities that have certain sets of components on them.

You might assume this is for performance reasons, after all ECS is often touted as allowing much higher efficiency compared to the usual object-oriented game object or heavy component approach.

That is not quite the case, however.

I chose ECS because I believe the separation of state and logic that ECS enforces (components vs systems) actually simplifies a lot of things. For instance, it is fairly simple to devise a RenderingSystem that has easy access to all of the cameras in the world, all of the static and skinned models, etc and can choose how it renders all of them (for instance, choosing to use instancing to render batches of objects all at once).

Think about this from the point of view of a traditional component-based architecture - where does the world get rendered? Does each mesh component render itself? If so, how do you deal with multiple cameras? Does each camera component need to know about all of the renderable components in the world? You quickly find yourself having to implement your logic outside of the component to handle all of this - think about how many times in Unity you end up writing "singleton" game objects with components on them to manage global state. So why not choose a system that is built around supporting that?