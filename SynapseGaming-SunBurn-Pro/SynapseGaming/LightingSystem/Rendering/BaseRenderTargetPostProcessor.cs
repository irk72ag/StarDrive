﻿// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.BaseRenderTargetPostProcessor
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using System.Collections.Generic;

namespace SynapseGaming.LightingSystem.Rendering
{
  /// <summary>
  /// Post-processor base class used for processors that contain their own
  /// render targets. These processors do *not* render directly to their own
  /// targets, instead the previous processor or the main scene rendering
  /// code (if no previous processor exists) renders to the target.  The current
  /// processor then uses a texture containing the image of it's target to
  /// apply visual effects to the next processor's render target.
  /// 
  /// Processors derived from this class are responsible for:
  ///   -switching to the ProcessorRenderTarget on BeginFrameRendering (done in the base method)
  ///   -getting and returning the texture attached to ProcessorRenderTarget in
  ///    EndFrameRendering (this is the unmodified source image)
  ///   -switching to and applying image data to the PreviousRenderTarget in EndFrameRendering
  ///    (this modifies the image data for the next processor in the chain)
  /// </summary>
  public abstract class BaseRenderTargetPostProcessor : IPostProcessor
  {
    private IGraphicsDeviceService igraphicsDeviceService_0;
    private RenderTarget2D renderTarget2D_0;
    private RenderTarget2D renderTarget2D_1;
    private Viewport Viewport;
    private ISceneState isceneState_0;

    /// <summary>
    /// Render target formats supported by the post processor.
    /// </summary>
    public abstract SurfaceFormat[] SupportedTargetFormats { get; }

    /// <summary>
    /// Source texture formats supported by the post processor. Source textures are
    /// provided by the previous post processor in the processing chain.
    /// </summary>
    public abstract SurfaceFormat[] SupportedSourceFormats { get; }

    /// <summary>
    /// The current GraphicsDeviceManager used by this object.
    /// </summary>
    protected IGraphicsDeviceService GraphicsDeviceManager
    {
      get
      {
        return this.igraphicsDeviceService_0;
      }
    }

    /// <summary>
    /// The processor's render target used for applying visual effects.
    /// </summary>
    protected RenderTarget2D ProcessorRenderTarget
    {
      get
      {
        return this.renderTarget2D_0;
      }
    }

    /// <summary>
    /// The render target occupying slot 0 in the device (either the next post processor's render
    /// target or the back buffer) prior to making the processor's render target the current target.
    /// </summary>
    protected RenderTarget2D PreviousRenderTarget
    {
      get
      {
        return this.renderTarget2D_1;
      }
    }

    /// <summary>
    /// The viewport in use by the device (either the next post processor's render
    /// target or the back buffer) prior to making the processor's viewport the current viewport.
    /// </summary>
    protected Viewport PreviousViewport
    {
      get
      {
        return this.Viewport;
      }
    }

    /// <summary>The current SceneState used by this object.</summary>
    protected ISceneState SceneState
    {
      get
      {
        return this.isceneState_0;
      }
    }

    /// <summary>Creates a BaseRenderTargetPostProcessor instance.</summary>
    /// <param name="graphicsdevicemanager"></param>
    public BaseRenderTargetPostProcessor(IGraphicsDeviceService graphicsdevicemanager)
    {
      this.igraphicsDeviceService_0 = graphicsdevicemanager;
    }

    /// <summary>
    /// Use to apply user quality and performance preferences to the resources managed by this object.
    /// </summary>
    /// <param name="preferences"></param>
    public abstract void ApplyPreferences(ILightingSystemPreferences preferences);

    /// <summary>
    /// Sets up the object prior to rendering. This base implementation automatically switches the current
    /// device render target to the processor's render target.
    /// </summary>
    /// <param name="scenestate"></param>
    public virtual void BeginFrameRendering(ISceneState scenestate)
    {
      GraphicsDevice graphicsDevice = this.igraphicsDeviceService_0.GraphicsDevice;
      this.Viewport = graphicsDevice.Viewport;
      this.renderTarget2D_1 = (RenderTarget2D) graphicsDevice.GetRenderTarget(0);
      graphicsDevice.SetRenderTarget(0, this.renderTarget2D_0);
      this.isceneState_0 = scenestate;
    }

    /// <summary>
    /// Applies post processing effects based on the source textures. This base implementation automatically
    /// sets the current device render target back to the previous target and returns a texture of the processor's render target data.
    /// </summary>
    /// <param name="mastersource">Texture containing the original scene without any visual processing applied.</param>
    /// <param name="lastprocessorsource">Texture containing the scene with visual processing applied by each
    /// previous post processor in the processing chain.</param>
    /// <returns>Returns a texture containing the post processor's output image.</returns>
    public virtual Texture2D EndFrameRendering(Texture2D mastersource, Texture2D lastprocessorsource)
    {
      GraphicsDevice graphicsDevice = this.igraphicsDeviceService_0.GraphicsDevice;
      graphicsDevice.SetRenderTarget(0, this.renderTarget2D_1);
      graphicsDevice.Viewport = this.Viewport;
      return this.renderTarget2D_0.GetTexture();
    }

    /// <summary>
    /// Sets up the post processor and tries to find supported formats for its visual processing.
    /// </summary>
    /// <param name="availableformats">List of formats available based on support by all previous
    /// post processor in the processing chain.</param>
    /// <returns>Returns true if the post processor was properly initialized.</returns>
    public virtual bool Initialize(List<SurfaceFormat> availableformats)
    {
      if (this.renderTarget2D_0 != null)
      {
        this.renderTarget2D_0.Dispose();
        this.renderTarget2D_0 = (RenderTarget2D) null;
      }
      for (int index1 = 0; index1 < this.SupportedTargetFormats.Length; ++index1)
      {
        for (int index2 = 0; index2 < availableformats.Count; ++index2)
        {
          if (this.SupportedTargetFormats[index1] == availableformats[index2])
          {
            GraphicsDevice graphicsDevice = this.igraphicsDeviceService_0.GraphicsDevice;
            this.renderTarget2D_0 = new RenderTarget2D(graphicsDevice, graphicsDevice.Viewport.Width, graphicsDevice.Viewport.Height, 1, this.SupportedTargetFormats[index1], graphicsDevice.PresentationParameters.MultiSampleType, graphicsDevice.PresentationParameters.MultiSampleQuality, LightingSystemManager.Instance.GetBestRenderTargetUsage());
            return true;
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Disposes any graphics resources used internally by this object.
    /// </summary>
    public virtual void Unload()
    {
      if (this.renderTarget2D_0 != null)
      {
        this.renderTarget2D_0.Dispose();
        this.renderTarget2D_0 = (RenderTarget2D) null;
      }
      this.renderTarget2D_1 = (RenderTarget2D) null;
    }
  }
}
