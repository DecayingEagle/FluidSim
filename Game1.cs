﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FluidSim {
  public class Circle {
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public Color Color { get; set; }
    public bool Colliding { get; set; }
    public float Radius { get; }
    public float Restitution { get; set; } 

    public Circle(Vector2 position, Color color, Vector2 velocity, float radius, float restitution) {
      Position = position;
      Color = color;
      Velocity = velocity;
      Radius = radius;
      Restitution = restitution;
    }
    
    // Check if this circle collides with another circle
    public bool CollidesWith(Circle other)
    {
      // Calculate the distance between the centers of the circles
      float deltaX = other.Position.X - Position.X;
        
      // Adjust the y-coordinate to consider the top edge of the circle
      float deltaY = (other.Position.Y + other.Radius) - (Position.Y + Radius);

      // Calculate the distance squared
      float distanceSquared = deltaX * deltaX + deltaY * deltaY;

      // Calculate the sum of the radii squared
      float radiiSquared = (Radius + other.Radius) * (Radius + other.Radius);

      // Check for collision
      return distanceSquared < radiiSquared -2;
    }
  }

  public class Game1 : Game {
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _circleTexture; // Texture for the circle
    private float _dt;

    private int _numberOfCircles = 100; // Set the desired number of circles
    private int _radiusOfCircles = 5;
    private float _gravity = 100f;
    private float _restitution = 0.90f;
    private Circle[] _circles;
    private Circle _playerCircle; // Array to store circle objects

    public Game1() {
      _graphics = new GraphicsDeviceManager(this);
      Content.RootDirectory = "Content";
      IsMouseVisible = true;
    }

    protected override void Initialize() {
      // Initialize circle objects
      _circles = new Circle[_numberOfCircles];
      _playerCircle = new Circle(new Vector2(0, 0), Color.Black, new Vector2(0), 10, 0);
      Random random = new Random();
      for (int i = 0; i < _numberOfCircles; i++) {
        Vector2 position = new Vector2(random.Next(0, _graphics.PreferredBackBufferWidth/10)*10, random.Next(0, _graphics.PreferredBackBufferHeight/10)*10);
        Color color = new Color(random.Next(256), random.Next(256), random.Next(256)); // You can set initial color here
        _circles[i] = new Circle(position, color, new Vector2(random.Next(-10, 10),random.Next(0, 10)), _radiusOfCircles, _restitution);
        
      }
      
      base.Initialize();
    }

    protected override void LoadContent() {
      _spriteBatch = new SpriteBatch(GraphicsDevice);

      // Create a circle texture with a fixed radius of 5
      _circleTexture = CreateCircleTexture(_radiusOfCircles);
    }

    private Texture2D CreateCircleTexture(int radius) {
      int diameter = radius * 2;
      Texture2D texture = new Texture2D(GraphicsDevice, diameter, diameter);
      Color[] data = new Color[diameter * diameter];

      float centerX = radius - 0.5f;
      float centerY = radius - 0.5f;
      float radiusSquared = radius * radius;

      for (int y = 0; y < diameter; y++) {
        for (int x = 0; x < diameter; x++) {
          int index = x + y * diameter;
          float distanceSquared = (x - centerX) * (x - centerX) + (y - centerY) * (y - centerY);

          // Set pixel color to white if inside the circle, otherwise set to transparent
          data[index] = (distanceSquared <= radiusSquared) ? Color.White : Color.Transparent;
        }
      }

      texture.SetData(data);
      return texture;
    }

    protected override void Update(GameTime gameTime) {
      if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
          Keyboard.GetState().IsKeyDown(Keys.Escape))
        Exit();
      _dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
      // Update your fluid simulation logic here
      _playerCircle.Position = Mouse.GetState().Position.ToVector2();
      foreach (Circle circle in _circles) {
        CheckBorderCollisions(circle);
        CheckCollisions(circle);
        CheckCollisions(circle, _playerCircle);
        circle.Velocity += new Vector2(0, _gravity * _dt);
        if (circle.Colliding) {
          circle.Velocity = -circle.Velocity * circle.Restitution;
        }
        circle.Position += circle.Velocity*_dt; // Update gravity
        
        
      }

      foreach (Circle circle in _circles) {
        circle.Colliding = false;
      }
      base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime) {
      GraphicsDevice.Clear(Color.CornflowerBlue);

      _spriteBatch.Begin();

      // Draw circles
      foreach (Circle circle in _circles) {
        _spriteBatch.Draw(_circleTexture, circle.Position, circle.Color);
      }
      _spriteBatch.Draw(_circleTexture,_playerCircle.Position,_playerCircle.Color);
      _spriteBatch.End();

      base.Draw(gameTime);
    }
    
    private void CheckCollisions(Circle currentCircle) {
      foreach (Circle otherCircle in _circles) {
        if (currentCircle != otherCircle && currentCircle.CollidesWith(otherCircle)) {
          // Handle collision as needed
          // For example, change colors or modify velocities
          currentCircle.Colliding = true;
          otherCircle.Colliding = true;
        }
      }
    }
    
    private void CheckCollisions(Circle currentCircle, Circle secondCircle) {
      if (currentCircle != secondCircle && currentCircle.CollidesWith(secondCircle)) {
        currentCircle.Colliding = true;
      }
    }
    private void CheckBorderCollisions(Circle circle) {
      // Adjust positions or velocities based on window borders
      if (circle.Position.X <= 0) {
        // Left border
        circle.Position = new Vector2(0, circle.Position.Y);
        circle.Velocity = new Vector2(Math.Abs(circle.Velocity.X) * circle.Restitution, circle.Velocity.Y); // Reverse the X velocity
      }
      if (circle.Position.X + circle.Radius * 2 > GraphicsDevice.Viewport.Width) {
        // Right border
        circle.Position = new Vector2(GraphicsDevice.Viewport.Width - circle.Radius * 2, circle.Position.Y);
        circle.Velocity = new Vector2(-Math.Abs(circle.Velocity.X) * circle.Restitution, circle.Velocity.Y); // Reverse the X velocity
      }
      if (circle.Position.Y <= 0) {
        // Top border
        circle.Position = new Vector2(circle.Position.X, 0);
        circle.Velocity = new Vector2(circle.Velocity.X, Math.Abs(circle.Velocity.Y) * circle.Restitution); // Reverse the Y velocity
      }
      if (circle.Position.Y + circle.Radius * 2 > GraphicsDevice.Viewport.Height) {
        // Bottom border
        circle.Position = new Vector2(circle.Position.X, GraphicsDevice.Viewport.Height - circle.Radius * 2);
        circle.Velocity = new Vector2(circle.Velocity.X, -Math.Abs(circle.Velocity.Y) * circle.Restitution); // Reverse the Y velocity
      }
    }
  }
}