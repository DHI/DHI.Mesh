# DHI.Mesh

This library contains data structures for easing implementing algorithms on mesh type data, i.e. unstructured, triangulated data - it can be quad elements as well.

From within DHI, mesh type data is present in the .mesh file and in the .dfsu files.

The following algorithms are currently implemented:
 * Extract mesh boundary
 * Interpolation: Calculate values in space, interpolating based on mesh element center values.

### Extract mesh boundary
For a given mesh, the boundary is extracted. A line for each boundary-code is returned.

### Interpolation
For mesh values defined at element center, DHI.Mesh can calculate an interpolated value anywhere within the mesh. The approach is as follows:
 * Node values are calculated based on a Pseudo-Laplacian interpolation procedure.
 * for a given point, interpolation using node and element center values are applied, such that:
   * If a value in the center of an element is requested, the element center value is returned.
   * On the straight line between two element center values, linear interpolation between
     these two element center values are returned. 
   * Left or right of this straight line between two element center values, the nearest node
     value is also taken into account in the interpolation, doing triangular interpolation.

This approach can be used for interpolation of data from one mesh to another. 
