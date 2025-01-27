﻿// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.Deferred.DeferredShadowMapManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Shadows.Deferred
{
  /// <summary>
  /// Manages scene shadow maps and provides methods for building and organizing
  /// relationships between lights and shadows.
  /// </summary>
  public class DeferredShadowMapManager : BaseShadowMapManager
  {
    private DisposablePool<DeferredShadowCubeMap> class22_1 = new DisposablePool<DeferredShadowCubeMap>();
    private DisposablePool<DeferredShadowDirectionalMap> class22_2 = new DisposablePool<DeferredShadowDirectionalMap>();

    /// <summary>Creates a new DeferredShadowMapManager instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    /// <param name="pagesize">Size in pixels of each render target (page) in the cache.
    /// For a size of 1024 the actual page dimensions are 1024x1024. Small sizes can reduce
    /// performance by fragmenting the shadow maps, and reduce shadow quality by lowering
    /// the maximum resolution of each shadow map section.</param>
    /// <param name="maxmemoryusage">Maximum amount of memory the cache is allowed to consume.
    /// This is an approximate value and the cache may use more memory in certain instances.</param>
    /// <param name="preferhalffloat">True when smaller half-float format render targets are
    /// preferred. These formats consume less memory and generally perform better, but have
    /// lower accuracy on directional lights.</param>
    public DeferredShadowMapManager(IGraphicsDeviceService graphicsdevicemanager, int pagesize, int maxmemoryusage, bool preferhalffloat)
      : base(graphicsdevicemanager, pagesize, maxmemoryusage, preferhalffloat)
    {
    }

    /// <summary>Creates a new DeferredShadowMapManager instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    /// <param name="shadowmapcache"></param>
    public DeferredShadowMapManager(IGraphicsDeviceService graphicsdevicemanager, ShadowMapCache shadowmapcache)
      : base(graphicsdevicemanager, shadowmapcache)
    {
    }

    /// <summary>Creates a new DeferredShadowMapManager instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    public DeferredShadowMapManager(IGraphicsDeviceService graphicsdevicemanager)
      : base(graphicsdevicemanager)
    {
    }

    /// <summary>
    /// Creates a new or cached shadow map object for this light type.
    /// </summary>
    /// <param name="shadowsource">Shadow source which uses the newly created or cached shadow map object.
    /// Provides information about how the shadow is used, such as location and the type of objects rendered
    /// to the shadow map.</param>
    /// <returns></returns>
    protected override IShadowMap CreateDirectionalShadowMap(IShadowSource shadowsource)
    {
      return this.class22_2.New();
    }

    /// <summary>
    /// Creates a new or cached shadow map object for this light type.
    /// </summary>
    /// <param name="shadowsource">Shadow source which uses the newly created or cached shadow map object.
    /// Provides information about how the shadow is used, such as location and the type of objects rendered
    /// to the shadow map.</param>
    /// <returns></returns>
    protected override IShadowMap CreatePointShadowMap(IShadowSource shadowsource)
    {
      return this.class22_1.New();
    }

    /// <summary>
    /// Creates a new or cached shadow map object for this light type.
    /// </summary>
    /// <param name="shadowsource">Shadow source which uses the newly created or cached shadow map object.
    /// Provides information about how the shadow is used, such as location and the type of objects rendered
    /// to the shadow map.</param>
    /// <returns></returns>
    protected override IShadowMap CreateSpotShadowMap(IShadowSource shadowsource)
    {
      return this.class22_1.New();
    }

    /// <summary>
    /// Finalizes rendering and cleans up frame information including removing all frame lifespan objects.
    /// </summary>
    public override void EndFrameRendering()
    {
      base.EndFrameRendering();
      this.class22_1.RecycleAllTracked();
      this.class22_2.RecycleAllTracked();
    }

    /// <summary>
    /// Unloads all scene and device specific data.  Must be called
    /// when the device is reset (during Game.UnloadGraphicsContent()).
    /// </summary>
    public override void Unload()
    {
      base.Unload();
      this.class22_1.Clear();
      this.class22_2.Clear();
    }
  }
}
