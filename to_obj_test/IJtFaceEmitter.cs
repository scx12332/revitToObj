using Autodesk.Revit.DB;

namespace to_obj_test
{
    interface IJtFaceEmitter
    {

        /// <summary>
        /// Emit a face with a specified colour.
        /// </summary>
        int EmitFace(
          Face face,
          Color color, // 3 bytes
          int shininess,
          int transparency); // [0,100]

        int GetFaceCount();
        int GetTriangleCount();
        int GetVertexCount();
    }
}