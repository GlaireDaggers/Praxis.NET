# Praxis Game Engine

A C# ECS-based game engine.

Under heavy construction, check back later!

## Goals

The primary governing goal of Praxis is that *it must be kept minimal*. In particular:

- Graphics ought to be very simple - basic forward rendering and a simple post FX stack. No PBR or fancy lighting here - there's at least three other engines with bigger communities and better docs you can use if you want that.
- The engine and the tools should be kept 100% separate. The engine doesn't really care where its content files come from, and the tools do not care what engine is consuming the files they produce. I *do not want* a big heavy monolithic editor. Case in point: Open World Builder will be the canonical level editor.
- The engine relies on several third party libraries, and should generally not attempt to hide or abstract that from you. The engine is built on FNA, and so the whole FNA API is available to you.

In short: Praxis can't compete with Godot, Unity, or Unreal, so it *shouldn't try to*. It's designed for specific use cases, and more advanced cases are better left to those other engines.