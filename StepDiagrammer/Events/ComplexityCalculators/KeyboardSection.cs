using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace StepDiagrammer
{
  /// <summary>
  /// Represents a section (a group of keys) of a physical keyboard.
  /// Sections have a size, a center, and a handedness. When a key from 
  /// one section to another section is pressed, the distance between 
  /// those sections' centers is used to calculate the cost. Handedness is
  /// also used by heuristics in the algorithm to determine which hand likely 
  /// pressed the key.
  /// </summary>
   public class KeyboardSection
  {
    public static Size StandardKeySize { get; set; }
    List<PhysicalKey> keys = new List<PhysicalKey>();
    public Point Center { get; set; }
    public Size Size { get; set; }
    public Handedness Handedness { get; set; }

    /// <summary>
    /// Adds a standard-size key with no exposed edges.
    /// </summary>
    public void Add(Keys key)
    {
      Add(key, Sides.None);
    }

    /// <summary>
    /// Adds a standard-sized key.
    /// </summary>
    public void Add(Keys key, Sides exposedEdges)
    {
      Add(key, exposedEdges, StandardKeySize);
    }

    /// <summary>
    /// Adds a key with the specified name, edges, and size.
    /// </summary>
    public void Add(Keys key, Sides exposedEdges, Size size)
    {
      string keyName = key.ToString();
      if (keyName.Length == 1 && Char.IsLetter(keyName, 0))
      {
        keys.Add(new PhysicalKey(keyName.ToUpper(), exposedEdges, size, this));
        keys.Add(new PhysicalKey(keyName.ToLower(), exposedEdges, size, this));
      }
      else 
        keys.Add(new PhysicalKey(keyName, exposedEdges, size, this));
    }

    public PhysicalKey FindKey(string keyName)
    {
      foreach (PhysicalKey key in keys)
        if (key.Name == keyName)
          return key;
        else if (keyName == "Enter" && key.Name == "Return")
          return key;
      return null;
    }

    public KeyboardSection(Point center, Size size, Handedness handedness)
    {
      Handedness = handedness;
      Size = size;
      Center = center;
    }
  }
}
