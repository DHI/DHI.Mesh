using System.Collections.Generic;
using NUnit.Framework;

namespace DHI.Mesh.Test
{
  [TestFixture]

  public class MeshDataTests
  {
    [Test]
    public void ElementIncludesTest()
    {
      MeshElement element = new MeshElement();
      element.Nodes = new List<MeshNode>(3);
      element.Nodes.Add(new MeshNode() { X = 1.1, Y = 1.0 });
      element.Nodes.Add(new MeshNode() { X = 2.2, Y = 1.1 });
      element.Nodes.Add(new MeshNode() { X = 1.6, Y = 2.0 });

      // Corner points are inside
      Assert.True(element.Includes(1.1, 1.0));
      Assert.True(element.Includes(2.2, 1.1));
      Assert.True(element.Includes(1.6, 2.0));

      // Points on face lines are inside
      // Mid point, first face
      Assert.True(element.Includes(1.65,  1.05));
      Assert.True(element.Includes(1.65,  1.05 + 0.0000001));
      Assert.False(element.Includes(1.65, 1.05 - 0.0000001));

      // Mid point, second face
      Assert.True(element.Includes(1.9,  1.55));
      Assert.True(element.Includes(1.9,  1.55 - 0.00000001));
      Assert.False(element.Includes(1.9, 1.55 + 0.00000001));

      // Mid point, third face
      Assert.True(element.Includes(1.35,  1.5));
      Assert.True(element.Includes(1.35,  1.5 - 0.000000001));
      Assert.False(element.Includes(1.35, 1.5 + 0.000000001));

    }

  }
}