using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
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
    public float Mass { get; set; }

    public Circle(Vector2 position, Color color, Vector2 velocity, float radius, float restitution, float mass) {
      Position = position;
      Color = color;
      Velocity = velocity;
      Radius = radius;
      Restitution = restitution;
      Mass = mass;
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
    private TimeSpan _dt;

    private int _numberOfCircles = 5000; // Set the desired number of circles
    private int _radiusOfCircles = 4;
    private float _gravity = 500f;
    private float _restitution = 0.95f;
    private float _mass = 1.0f;
    private List<Circle> _circles;
    private Circle _playerCircle; // Array to store circle objects
    
    [Flags]
    private enum MouseButtonsPressed {
      None = 0b_00,
      Left = 0b_10,
      Right = 0b_01
    }

    private MouseButtonsPressed _pressed;
#if DEBUG
    private static bool _debugFlag = true;
#endif

    public Game1() {
      _graphics = new GraphicsDeviceManager(this);
      Content.RootDirectory = "Content";
      IsMouseVisible = true;
    }

    protected override void Initialize() {
      // Initialize circle objects
      _circles = new List<Circle>(_numberOfCircles);
      _playerCircle = new Circle(new Vector2(0, 0), Color.Black, new Vector2(0), 10, 0, 0f);
      Random random = new Random();
      for (int i = 0; i < _numberOfCircles; i++) {
        Vector2 position = new Vector2(random.Next(0, _graphics.PreferredBackBufferWidth/10)*10, random.Next(0, _graphics.PreferredBackBufferHeight/10)*10);
        Color color = new Color(random.Next(256), random.Next(256), random.Next(256)); // You can set initial color here
        _circles.Add(new Circle(position, color, new Vector2(random.Next(-10, 10),random.Next(0, 10)), _radiusOfCircles, _restitution, _mass));
      }
      base.Initialize();
    }

    protected override void LoadContent() {
      _spriteBatch = new SpriteBatch(GraphicsDevice);
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

    protected override async void Update(GameTime gameTime) {

      if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
          Keyboard.GetState().IsKeyDown(Keys.Escape)) {
        UnloadContent();
        Exit();
      } else if(this.IsActive | _debugFlag) await ComputeCirclesAsyncParallel(_circles);

      if (Mouse.GetState().LeftButton == ButtonState.Released) {
        _pressed &= ~MouseButtonsPressed.Left;
      }

      if ((Mouse.GetState().LeftButton == ButtonState.Pressed)) {
        _pressed |= MouseButtonsPressed.Left;
      }
      
      if (_pressed.HasFlag(MouseButtonsPressed.Left)) {
        _circles.Remove(_playerCircle);
        SpawnCircleOnClick(Mouse.GetState().X, Mouse.GetState().Y);
      }

      // Update your fluid simulation logic here
      _playerCircle.Position = Mouse.GetState().Position.ToVector2();
      
      base.Update(gameTime);
      _dt = gameTime.ElapsedGameTime;
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
    
    protected override void UnloadContent()
    {
      _circleTexture.Dispose();
      // Dispose of other resources if needed

      base.UnloadContent();
    }

    private async Task<List<Circle>> ComputeCirclesAsyncParallel(List<Circle> list) {
      await Task.Run(() => {
        Parallel.ForEach(list, circle => {
          CheckBorderCollisions(circle);
          CheckCollisions(circle);
          circle.Velocity += new Vector2(0, _gravity * (float)_dt.TotalSeconds);
          if (circle.Colliding) {
            circle.Velocity = -circle.Velocity * circle.Restitution;
          }
          circle.Position += circle.Velocity*(float)_dt.TotalSeconds; // Update gravity
        });
      });
      
      foreach (Circle circle in list) {
        circle.Colliding = false;
      }

      return list;
    }
    
    private void CheckCollisions(Circle currentCircle)
    {
      foreach (Circle otherCircle in _circles)
      {
        if (currentCircle != otherCircle && currentCircle.CollidesWith(otherCircle))
        {
          HandleCollision(currentCircle, otherCircle);
        }
      }
    }
    
    //TODO: Possibly have to remove this method later on.
    private void CheckCollisions(Circle currentCircle, Circle otherCircle)
    {
      if (currentCircle != otherCircle && currentCircle.CollidesWith(otherCircle)) {
        HandleCollision(currentCircle, otherCircle);
      }
    }

    private void HandleCollision(Circle circle1, Circle circle2)
    {
      // Calculate relative velocity
      Vector2 relativeVelocity = circle2.Velocity - circle1.Velocity;

      // Calculate the normal vector (direction from circle1 to circle2)
      Vector2 normal = Vector2.Normalize(circle2.Position - circle1.Position);

      // Calculate the relative velocity along the normal
      float relativeSpeed = Vector2.Dot(relativeVelocity, normal);

      // Calculate the impulse (change in momentum)
      float impulse = (2.0f * relativeSpeed) / (circle1.Mass + circle2.Mass);

      // Apply the impulse to update velocities
      circle1.Velocity = circle1.Velocity + impulse * circle2.Mass * normal;
      circle2.Velocity = circle2.Velocity - impulse * circle1.Mass * normal;

      // Apply restitution to simulate elasticity
      circle1.Velocity *= circle1.Restitution;
      circle2.Velocity *= circle2.Restitution;

      // Optionally, you may want to move circles apart to avoid continuous collisions
      SeparateCircles(circle1, circle2);
    }

    private void SeparateCircles(Circle circle1, Circle circle2)
    {
      // Move circles apart to avoid overlap
      float overlap = circle1.Radius + circle2.Radius - Vector2.Distance(circle1.Position, circle2.Position);
      Vector2 separation = Vector2.Normalize(circle1.Position - circle2.Position) * overlap * 0.5f;

      circle1.Position += separation;
      circle2.Position -= separation;
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

    private void SpawnCircleOnClick(int x, int y) {
      Debug.WriteLine(x);
      Debug.WriteLine(y);
      _circles.Add(new Circle(new Vector2(x, y), Color.Red, new Vector2(0), _radiusOfCircles, _restitution, _mass));
    }
  }
}