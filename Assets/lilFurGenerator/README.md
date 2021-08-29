# lilFurGenerator
Version 1.0.1

# Overview
A plug-in that generates a fur mesh.  
Since the fur is pre-generated in the editor, it is useful in environments where the GPU is the bottleneck and geometry shaders are not available.  
The lighting is adapted to lilToon so that there is no difference in brightness.

# Support
Supported Unity versions
- Unity 2018 - Unity 2021.2

Supported Shader Models
- SM4.0 / ES3.0 or later

Supported Rendering Pipelines
- Built-in Render Pipeline (BRP)
- Lightweight Render Pipeline (LWRP)
- Universal Render Pipeline (URP)

# Features
- Fur mesh generator
- Adjust length, vector, and stiffness using textures
- Optimized shader for the generated meshes

# License
lilFurGenerator is available under the MIT License. Please refer to the `LICENSE` included in the package.

# Usage
1. Import lilFurGenerator into Unity using one of the following methods.  
    i. Drag and drop unitypackage to the Unity window to import it.  
    ii. Import ```https://github.com/lilxyzw/lilFurGenerator.git?path=Assets/lilFurGenerator#master``` from UPM.  
2. Select `Window/_lil/Fur Generator` from the top menu bar
3. Select the mesh for which you want to generate fur in the mesh properties
4. Save meshes and materials (Save button will appear red if they have not been saved)
5. Delete the original mesh or change the tag to `EditorOnly`

Select and edit materials for change advanced settings.

# Advanced Properties
|Name|Description|
|-|-|
|Frequency|Wind frequency of each XYZ axes.|
|Strength|Wind strength of each XYZ axes.|
|Detail|Randomness of wind direction.|
|Touch Strength|Strength of deformation by collider using vertex light.|

Point Light used for the touch function works by setting as follows.  
Due to Unity's limits, you can only touch up to 4 lights.  
Prefab asset that has already been set is included in the `Prefabs` folder.
|Name|Description|
|-|-|
|Range|Any (the size of the Collider)|
|Color|RGBA = (0.0, 0.0, 0.0, 0.055)|
|Mode|Realtime|
|Intensity|1|
|Render Mode|Not Important|

# Bug Report
If you have any other problems and suspect a bug, please contact me on [Twitter](https://twitter.com/lil_xyzw), [GitHub](https://github.com/lilxyzw/lilFurGenerator), or [BOOTH](https://lilxyzw.booth.pm/).  
Please refer to the following template when reporting a bug.
```
Bug: 
Reproduction method: 

# Optional
Unity version: 
Shader setting: 
VRChat World: 
Screenshots: 
Console logs: 
```

# References
- [Unity で URP 向けのファーシェーダを書いてみた（フィン法）](https://tips.hecomi.com/entry/2021/07/24/121420)  
- [UnlitWF (whiteflare)](https://github.com/whiteflare/Unlit_WF_ShaderSuite) / [MIT LICENCE](https://github.com/whiteflare/Unlit_WF_ShaderSuite/blob/master/LICENSE)  

# For Developer
This plug-in can be used in other shaders by performing the same process.  
Shaders with `FurGenerator` in their display name will be added to the list in the editor window. (display name, not file name)  
See `lilFurGenerator/Shader/lilFurGeneratorUnlit.shader` for fur calculations.

# Change log
## v1.0.1
- Add color code next to HDR color picker
- Added touch function
- Fixed UPM import
- Fixed error in Unity 2019 URP
- Added shader settings for optimization
- Moved some processing to the vertex shader for optimization
- Changed transform calculation
## v1.0.0
- Opening to the public