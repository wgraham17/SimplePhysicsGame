﻿// -----------------------------------------------------------------------
// <copyright file="SimulatedLight.cs" company="">
// Copyright (C) 2011-2013 Matthew Razza
// </copyright>
// -----------------------------------------------------------------------

namespace CelestialEngine.Core.PostProcess
{
    using System.Collections.Generic;
    using CelestialEngine.Core;
    using CelestialEngine.Core.Shaders;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using System.Linq;

    /// <summary>
    /// Abstract base class for all simulated light post processing effects.
    /// This class provides shared functionality among all simulated lights.
    /// </summary>
    public abstract class SimulatedLight : SimulatedPostProcess
    {
        #region Members
        /// <summary>
        /// The change in Height in game units/second
        /// </summary>
        private float lightHeightVelocity;

        /// <summary>
        /// The position of the light
        /// </summary>
        private Vector3 lightPosition;

        /// <summary>
        /// The shader used when generating the shadow map.
        /// </summary>
        private Shader shadowMapShader;

        /// <summary>
        /// If true this light will cast shadows; otherwise the light will ignore shadow casting sprites
        /// </summary>
        private bool castsShadows;

        /// <summary>
        /// The distance, in world units, until the shadow edge is fully blurred
        /// </summary>
        private float maxShadowBlurDistance;

        /// <summary>
        /// The distance, in world units, until the shadow edge starts blurring
        /// </summary>
        /// <remarks>A value of float.PositiveInfinity means no shadow blur enabled</remarks>
        private float minShadowBlurDistance;

        /// <summary>
        /// The layer depth for this light (used for shadows)
        /// </summary>
        private float layerDepth;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatedLight"/> class.
        /// </summary>
        /// <param name="world">The <see cref="World" /> in which the instance lives.</param>
        /// <param name="shader">The shader to use with the light.</param>
        public SimulatedLight(World world, Shader shader)
            : base(world, shader)
        {
            this.lightPosition = Vector3.Zero;
            this.shadowMapShader = new Shader(Content.Shaders.Core.ShadowMap);
            this.maxShadowBlurDistance = float.PositiveInfinity;
            this.minShadowBlurDistance = float.PositiveInfinity;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the distance from the light source until the shadow edge is fully blurred.
        /// </summary>
        /// <remarks>To disable soft shadow edges, set MinShadowBlurDistance to float.PositiveInfinity.</remarks>
        /// <value>The shadow blur distance.</value>
        public float MaxShadowBlurDistance
        {
            get
            {
                return this.maxShadowBlurDistance;
            }

            set
            {
                this.maxShadowBlurDistance = value;
            }
        }

        /// <summary>
        /// Gets or sets the distance, in world units, from the light source until the shadow begins to blur. To disable soft shadow edges, set to float.PositiveInfinity.
        /// <seealso cref="MaxShadowBlurDistance"/>
        /// </summary>
        /// <value>The distance from the light source until bluring begins</value>
        public float MinShadowBlurDistance
        {
            get
            {
                return this.minShadowBlurDistance;
            }

            set
            {
                this.minShadowBlurDistance = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Vector3"/> calculated position of the object in absolute 2D world space.
        /// </summary>
        /// <value>
        /// The position of the object in absolute 2D world space.
        /// </value>
        public new Vector3 Position
        {
            get
            {
                return this.lightPosition;
            }

            set
            {
                this.lightPosition = value;
                base.Position = new Vector2(value.X, value.Y);
            }
        }

        /// <summary>
        /// Gets the <see cref="Vector3"/> calculated position of the object in absolute 2D pixel space.
        /// </summary>
        /// <value>The position of the object in absolute 2D pixel space.</value>
        public new Vector3 PixelPosition
        {
            get
            {
                return this.World.GetPixelFromWorld(this.lightPosition);
            }
        }

        /// <summary>
        /// Gets or sets the velocity of the object in world space units/sec.
        /// (delta position over time)
        /// </summary>
        /// <value>
        /// The velocity of the object in units/sec.
        /// </value>
        public new Vector3 Velocity
        {
            get
            {
                return new Vector3(base.Velocity, this.lightHeightVelocity);
            }

            set
            {
                base.Velocity = new Vector2(value.X, value.Y);
                this.lightHeightVelocity = value.Z;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not this light should casts shadows.
        /// </summary>
        /// <value><c>true</c> if this light will casts shadows; otherwise, <c>false</c>.</value>
        public bool CastsShadows
        {
            get
            {
                return this.castsShadows;
            }

            set
            {
                this.castsShadows = value;
            }
        }

        /// <summary>
        /// Gets or sets the layer depth for this light (used when calculating which objects are in shadow).
        /// </summary>
        /// <value>
        /// The layer depth.
        /// </value>
        public float LayerDepth
        {
            get
            {
                return this.layerDepth;
            }

            set
            {
                this.layerDepth = value;
            }
        }
        #endregion

        #region SimulatedPostProcess Overrides
        /// <summary>
        /// Called when graphics resources need to be loaded. Override this method to load any component-specific graphics resources.
        /// </summary>
        /// <param name="contentManager">The content manager.</param>
        public override void LoadContent(ExtendedContentManager contentManager)
        {
            this.shadowMapShader.LoadContent(contentManager);
            base.LoadContent(contentManager);
        }

        /// <summary>
        /// Called when the GameComponent needs to be updated. Override this method with component-specific update code.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Update position
            this.lightPosition.X = base.Position.X;
            this.lightPosition.Y = base.Position.Y;
            this.lightPosition.Z += this.lightHeightVelocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Generates the shadow map to be used when rendering this light.
        /// </summary>
        /// <remarks>If this instance is not to cast shadows the shadow map is cleared.</remarks>
        /// <param name="renderSystem">The render system to render with.</param>
        /// <param name="shadowMap">The shadow map to render on</param>
        protected virtual void GenerateShadowMap(DeferredRenderSystem renderSystem, RenderTarget2D shadowMap)
        {
            RenderTarget2D intermediateShadowMap = renderSystem.RenderTargets.GetTemporaryRenderTarget(SurfaceFormat.Color); // Intermediate map used for selective sampling
            RectangleF lightDrawBounds = this.GetWorldDrawBounds();

            renderSystem.GraphicsDevice.SetRenderTarget(shadowMap);
            renderSystem.ClearCurrentRenderTarget(Color.White); // Clear the core shadow map

            // Only render the shadows if we are to cast them
            if (this.CastsShadows)
            {
                // Render the shadow caused by each object within the lights range
                List<SpriteBase> objectList = this.World.GetSpriteObjectsInArea(lightDrawBounds).Where(currObj => (currObj.RenderOptions & SpriteRenderOptions.CastsShadows) != 0).OrderBy(s => s.LayerDepth).ToList();

                if (objectList.Count > 0)
                {
                    int lastLayerDepth = 0;
                    int lastLayerDepthIndex = 0;
                    bool renderedShadow = false; // This is used so that we don't bother occluding objects if we didn't even render anything

                    lastLayerDepth = (int)objectList[0].LayerDepth;

                    for (int objectIndex = 0; objectIndex < objectList.Count; objectIndex++)
                    {
                        SpriteBase currObj = objectList[objectIndex];
                        RectangleF spriteWorldBounds = currObj.SpriteWorldBounds;

                        // If the object we found doesn't cast shadows (or we're inside it), skip it
                        if (spriteWorldBounds.Contains(base.Position))
                        {
                            continue;
                        }

                        this.shadowMapShader.ConfigureShader(renderSystem); // Configure the shader
                        this.shadowMapShader.GetParameter("viewProjection").SetValue(renderSystem.GameCamera.GetViewProjectionMatrix(renderSystem));
                        this.shadowMapShader.GetParameter("cameraPosition").SetValue(renderSystem.GameCamera.PixelPosition);

                        // If we switched light planes remove occlusion from all previous objects
                        if (lastLayerDepth != (int)currObj.LayerDepth)
                        {
                            // Only occlude if we actually rendered something
                            if (renderedShadow)
                            {
                                // Loop through all the objects between the last layer index and the current index
                                for (int clearObjIndex = lastLayerDepthIndex; clearObjIndex < objectIndex; clearObjIndex++)
                                {
                                    this.ProcessSpriteDepthTransition(renderSystem, objectList[clearObjIndex]);
                                }
                            }

                            // Save the last values
                            lastLayerDepth = (int)currObj.LayerDepth;
                            lastLayerDepthIndex = objectIndex;
                            renderedShadow = false; // Reset this
                        }

                        // Create the shadow map caused by the current sprite
                        this.shadowMapShader.ApplyPass(0);

                        // Construct the vertex primitive to mask with
                        Vector2[] extrema;
                        if (currObj.SpriteWorldShape != null)
                        {
                            extrema = currObj.SpriteWorldShape.GetRelativeExtrema(base.Position); // Get the extrema that cause the shadow
                        }
                        else
                        {
                            extrema = spriteWorldBounds.GetRelativeExtrema(base.Position); // Get the extrema that cause the shadow
                        }

                        Vector2 widthVector = Vector2.Normalize(extrema[0] - base.Position); // Get the vector to the first extrema
                        Vector2 heightVector = Vector2.Normalize(extrema[1] - base.Position); // Get the vector to the second extrema

                        VertexPrimitive shadowArea = new VertexPrimitive(PrimitiveType.TriangleStrip, 4);
                        shadowArea.Add(extrema[0]);
                        shadowArea.Add(extrema[1]);

                        // Let's extend the shadow vector until it hits the edge of the draw bounds
                        shadowArea.Add(lightDrawBounds.CastInternalRay(extrema[0], widthVector));
                        shadowArea.Add(lightDrawBounds.CastInternalRay(extrema[1], heightVector));

                        // Let's get the remaining verts that might exist to finish up the rect
                        List<Vector2> interiorVerts = lightDrawBounds.GetInteriorVertices(base.Position, widthVector, heightVector);
                        shadowArea.Add(interiorVerts); // Add the interior verts (if any exist)

                        renderSystem.DirectScreenPaint(shadowArea); // Render the shadow
                        renderedShadow = true; // We just rendered a shadow; keep track of that
                    }
                    
                    // Loop through all the objects between the last layer index and the current index
                    if (renderedShadow)
                    {
                        // Remove the shadows over the LAST shadow plane
                        for (int clearObjIndex = lastLayerDepthIndex; clearObjIndex < objectList.Count; clearObjIndex++)
                        {
                            this.ProcessSpriteDepthTransition(renderSystem, objectList[clearObjIndex]);
                        }

                        // Blur the shadow map if enabled
                        if (!float.IsPositiveInfinity(this.minShadowBlurDistance))
                        {
                            renderSystem.GraphicsDevice.SetRenderTarget(intermediateShadowMap); // We need to render to a temp target

                            // Configure the shader
                            this.shadowMapShader.ConfigureShader(renderSystem, 1);

                            // Set parameters
                            this.shadowMapShader.GetParameter("viewProjection").SetValue(renderSystem.GameCamera.GetViewProjectionMatrix(renderSystem));
                            this.shadowMapShader.GetParameter("cameraPosition").SetValue(renderSystem.GameCamera.PixelPosition);
                            this.shadowMapShader.GetParameter("lightPosition").SetValue(base.PixelPosition);
                            this.shadowMapShader.GetParameter("maxBlurDistance").SetValue(this.World.GetPixelFromWorld(this.maxShadowBlurDistance));
                            this.shadowMapShader.GetParameter("minBlurDistance").SetValue(this.World.GetPixelFromWorld(this.maxShadowBlurDistance));
                            this.shadowMapShader.GetParameter("shadowMap").SetValue(shadowMap);

                            this.shadowMapShader.ApplyPass(0);
                            renderSystem.DirectScreenPaint(lightDrawBounds); // Render the shadow

                            renderSystem.GraphicsDevice.SetRenderTarget(shadowMap); // Set the shadow map for final render

                            // Update shadow map
                            this.shadowMapShader.GetParameter("shadowMap").SetValue(intermediateShadowMap);

                            this.shadowMapShader.ApplyPass(0);
                            renderSystem.DirectScreenPaint(lightDrawBounds); // Render
                        }
                    }
                }
            }

            renderSystem.SetRenderTargets(RenderTargetTypes.None, 0); // Resolve the render target
            renderSystem.RenderTargets.ReleaseTemporaryRenderTarget(intermediateShadowMap);
        }

        /// <summary>
        /// Removes a shadow from sprite or fully covers the sprite in shadow, depending on the layer depth transition.
        /// </summary>
        /// <param name="renderSystem">The render system.</param>
        /// <param name="sprite">The sprite.</param>
        private void ProcessSpriteDepthTransition(DeferredRenderSystem renderSystem, SpriteBase sprite)
        {
            // Remove the shadow map that overlaps with the sprite (or fully cover it)
            if (sprite.LayerDepth <= this.layerDepth)
            {
                this.shadowMapShader.ApplyPass(1); // Remove
            }

            if (sprite.SpriteWorldPrimitives != null)
            {
                foreach (var currShape in sprite.SpriteWorldPrimitives)
                {
                    renderSystem.DirectScreenPaint(currShape);
                }
            }
            else
            {
                renderSystem.DirectScreenPaint(sprite.SpriteWorldBounds);
            }
        }
        #endregion
    }
}
