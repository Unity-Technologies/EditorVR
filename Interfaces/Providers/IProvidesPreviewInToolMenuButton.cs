using System;
using Unity.XRTools.ModuleLoader;
using UnityEngine;

namespace Unity.EditorXR.Interfaces
{
    /// <summary>
    /// Provide the ability control the tool preview
    /// </summary>
    public interface IProvidesPreviewInToolMenuButton : IFunctionalityProvider
    {
      /// <summary>
      /// Highlights a ToolMenuButton when a menu button is highlighted
      /// </summary>
      /// <param name="rayOrigin">Transform: Ray origin to check</param>
      /// <param name="toolType">Type: MenuButton's tool type to preview</param>
      /// <param name="toolDescription">String: The tool description to display as a Tooltip</param>
      void PreviewInToolsMenuButton(Transform rayOrigin, Type toolType, string toolDescription);

      /// <summary>
      /// Clears any ToolMenuButton previews that are set
      /// </summary>
      void ClearToolsMenuButtonPreview();
    }
}
