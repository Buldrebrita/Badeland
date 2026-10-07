# Which renderer is the project using?

**Right now: Unity's built-in renderer, not URP.** That is not what TECH.md planned (URP), and it happened by accident:
when the Unity project was created in a temporary folder and only `ProjectSettings/` and `Packages/` were copied into the
repo, the temporary project's `Assets/Settings/` folder was left behind. That folder holds the URP settings files, so
the quality levels point at URP files that do not exist, and Unity falls back to the built-in renderer. You can see it in
`ProjectSettings/GraphicsSettings.asset` (`m_CustomRenderPipeline: {fileID: 0}`).

What that means:

- Materials that use the old built-in "Standard" shader draw fine. Materials made for URP show **magenta**.
- The scene builders therefore copy what Unity puts on a new primitive, so they work with either renderer. Materials
  that were made for the other renderer are switched over automatically the next time a scene is built.
- URP-only things do nothing yet: the post-processing look (bloom, colour grading, vignette), URP shaders, and Shader Graph.
  The builder skips the post-processing and says so in the Console.

## Switching to URP later (optional)

Do this when you want the nicer look. It is a few clicks:

1. In the Project window: right-click `Assets` > **Create > Rendering > URP Asset (with Universal Renderer)**.
2. **Edit > Project Settings > Graphics**: set *Default Render Pipeline* to that asset.
3. **Edit > Project Settings > Quality**: for each quality level, set *Render Pipeline Asset* to the same asset.
4. Convert the old materials: **Window > Rendering > Render Pipeline Converter**, pick *Built-in to URP*, tick *Material Upgrade*,
   *Initialize and Convert*. (Or just rebuild the scenes: the builders now repair materials for the active renderer.)
5. Rebuild the scenes with the Badeland menu. The post-processing look is then created too.
6. Commit the new `Assets/Settings` files with GitHub Desktop, so others get them.
