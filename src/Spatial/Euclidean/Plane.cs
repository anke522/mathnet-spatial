﻿namespace MathNet.Spatial.Euclidean
{
    using System;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using System.Xml.Serialization;
    using MathNet.Numerics.LinearAlgebra.Double;
    using MathNet.Spatial.Internals;
    using MathNet.Spatial.Units;

    [Serializable]
    public struct Plane : IEquatable<Plane>, IXmlSerializable
    {
        /// <summary>
        /// The normal vector of the Plane.
        /// </summary>
        public readonly UnitVector3D Normal;

        /// <summary>
        /// The distance to the Plane along its normal from the origin.
        /// </summary>
        public readonly double D;

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane"/> struct.
        /// Constructs a Plane from the X, Y, and Z components of its normal, and its distance from the origin on that normal.
        /// </summary>
        /// <param name="x">The X-component of the normal.</param>
        /// <param name="y">The Y-component of the normal.</param>
        /// <param name="z">The Z-component of the normal.</param>
        /// <param name="d">The distance of the Plane along its normal from the origin.</param>
        public Plane(double x, double y, double z, double d)
            : this(UnitVector3D.Create(x, y, z), -d)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane"/> struct.
        /// Constructs a Plane from the given normal and distance along the normal from the origin.
        /// </summary>
        /// <param name="normal">The Plane's normal vector.</param>
        /// <param name="offset">The Plane's distance from the origin along its normal vector.</param>
        public Plane(UnitVector3D normal, double offset = 0)
        {
            this.Normal = normal;
            this.D = -offset;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane"/> struct.
        /// Constructs a Plane from the given normal and distance along the normal from the origin.
        /// </summary>
        /// <param name="normal">The Plane's normal vector.</param>
        /// <param name="rootPoint">A point in the plane.</param>
        public Plane(UnitVector3D normal, Point3D rootPoint)
            : this(normal, normal.DotProduct(rootPoint))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane"/> struct.
        /// Constructs a Plane from the given normal and distance along the normal from the origin.
        /// </summary>
        /// <param name="normal">The Plane's normal vector.</param>
        /// <param name="rootPoint">A point in the plane.</param>
        public Plane(Point3D rootPoint, UnitVector3D normal)
            : this(normal, normal.DotProduct(rootPoint))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane"/> struct.
        /// Creates a Plane that contains the three given points.
        /// http://www.had2know.com/academics/equation-plane-through-3-points.html
        /// </summary>
        /// <param name="p1">The first point on the Plane.</param>
        /// <param name="p2">The second point on the Plane.</param>
        /// <param name="p3">The third point on the Plane.</param>
        /// <returns>The Plane containing the three points.</returns>
        [Obsolete("This constructor will be removed, use factory method Plane.FromPoints. Made obsolete 2017-12-05.")]
        public Plane(Point3D p1, Point3D p2, Point3D p3)
        {
            this = FromPoints(p1, p2, p3);
        }

        /// <summary>
        /// Gets the <see cref="Normal"/> x component.
        /// </summary>
        public double A => this.Normal.X;

        /// <summary>
        /// Gets the <see cref="Normal"/> y component.
        /// </summary>
        public double B => this.Normal.Y;

        /// <summary>
        /// Gets the <see cref="Normal"/> y component.
        /// </summary>
        public double C => this.Normal.Z;

        /// <summary>
        /// Gets the point on the plane closest to origin.
        /// </summary>
        public Point3D RootPoint => (-this.D * this.Normal).ToPoint3D();

        public static bool operator ==(Plane left, Plane right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Plane left, Plane right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane"/> struct.
        /// Creates a Plane that contains the three given points.
        /// http://www.had2know.com/academics/equation-plane-through-3-points.html
        /// </summary>
        /// <param name="p1">The first point on the Plane.</param>
        /// <param name="p2">The second point on the Plane.</param>
        /// <param name="p3">The third point on the Plane.</param>
        /// <returns>The Plane containing the three points.</returns>
        public static Plane FromPoints(Point3D p1, Point3D p2, Point3D p3)
        {
            if (p1 == p2 || p1 == p3 || p2 == p3)
            {
                throw new ArgumentException("Must use three different points");
            }

            var v1 = new Vector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
            var v2 = new Vector3D(p3.X - p1.X, p3.Y - p1.Y, p3.Z - p1.Z);
            var cross = v1.CrossProduct(v2);

            if (cross.Length <= float.Epsilon)
            {
                throw new ArgumentException("The 3 points should not be on the same line");
            }

            return new Plane(cross.Normalize(), p1);
        }

        /// <summary>
        /// Creates a Plane from its string representation
        /// </summary>
        /// <param name="s">The string representation of the Plane</param>
        /// <returns></returns>
        public static Plane Parse(string s)
        {
            return Parser.ParsePlane(s);
        }

        public double SignedDistanceTo(Point3D point)
        {
            var point3D = this.Project(point);
            var vectorTo = point3D.VectorTo(point);
            return vectorTo.DotProduct(this.Normal);
        }

        public double SignedDistanceTo(Plane otherPlane)
        {
            if (!this.Normal.IsParallelTo(otherPlane.Normal, tolerance: 1E-15))
            {
                throw new ArgumentException("Planes are not parallel");
            }

            return this.SignedDistanceTo(otherPlane.RootPoint);
        }

        public double SignedDistanceTo(Ray3D ray)
        {
            if (Math.Abs(ray.Direction.DotProduct(this.Normal) - 0) < 1E-15)
            {
                return this.SignedDistanceTo(ray.ThroughPoint);
            }

            return 0;
        }

        public double AbsoluteDistanceTo(Point3D point)
        {
            return Math.Abs(this.SignedDistanceTo(point));
        }

        public Point3D Project(Point3D p, UnitVector3D? projectionDirection = null)
        {
            var dotProduct = this.Normal.DotProduct(p.ToVector3D());
            var projectiononNormal = projectionDirection == null ? this.Normal : projectionDirection.Value;
            var projectionVector = (dotProduct + this.D) * projectiononNormal;
            return p - projectionVector;
        }

        public Line3D Project(Line3D line3DToProject)
        {
            var projectedStartPoint = this.Project(line3DToProject.StartPoint);
            var projectedEndPoint = this.Project(line3DToProject.EndPoint);
            return new Line3D(projectedStartPoint, projectedEndPoint);
        }

        public Ray3D Project(Ray3D rayToProject)
        {
            var projectedThroughPoint = this.Project(rayToProject.ThroughPoint);
            var projectedDirection = this.Project(rayToProject.Direction.ToVector3D());
            return new Ray3D(projectedThroughPoint, projectedDirection.Direction);
        }

        /// <summary>
        /// Project Vector3D onto this plane
        /// </summary>
        /// <param name="vector3DToProject">The Vector3D to project</param>
        /// <returns>The projected Vector3D</returns>
        public Ray3D Project(Vector3D vector3DToProject)
        {
            var projectedEndPoint = this.Project(vector3DToProject.ToPoint3D());
            var projectedZero = this.Project(new Point3D(0, 0, 0));
            return new Ray3D(projectedZero, projectedZero.VectorTo(projectedEndPoint).Normalize());
        }

        /// <summary>
        /// Project Vector3D onto this plane
        /// </summary>
        /// <param name="vector3DToProject">The Vector3D to project</param>
        /// <returns>The projected Vector3D</returns>
        public Ray3D Project(UnitVector3D vector3DToProject)
        {
            return this.Project(vector3DToProject.ToVector3D());
        }

        /// <summary>
        /// Finds the intersection of the two planes, throws if they are parallel
        /// http://mathworld.wolfram.com/Plane-PlaneIntersection.html
        /// </summary>
        /// <param name="intersectingPlane"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public Ray3D IntersectionWith(Plane intersectingPlane, double tolerance = float.Epsilon)
        {
            var a = new DenseMatrix(2, 3);
            a.SetRow(0, this.Normal.ToVector());
            a.SetRow(1, intersectingPlane.Normal.ToVector());
            var svd = a.Svd(true);
            if (svd.S[1] < tolerance)
            {
                throw new ArgumentException("Planes are parallel");
            }

            var y = new DenseMatrix(2, 1)
            {
                [0, 0] = -1 * this.D,
                [1, 0] = -1 * intersectingPlane.D
            };

            var pointOnIntersectionLine = svd.Solve(y);
            var throughPoint = Point3D.OfVector(pointOnIntersectionLine.Column(0));
            var direction = UnitVector3D.OfVector(svd.VT.Row(2));
            return new Ray3D(throughPoint, direction);
        }

        /// <summary>
        /// Find intersection between Line3D and Plane
        /// http://geomalgorithms.com/a05-_intersect-1.html
        /// </summary>
        /// <param name="line"></param>
        /// <param name="tolerance"></param>
        /// <returns>Intersection Point or null</returns>
        public Point3D? IntersectionWith(Line3D line, double tolerance = float.Epsilon)
        {
            if (line.Direction.IsPerpendicularTo(this.Normal)) // either parallel or lies in the plane
            {
                var projectedPoint = this.Project(line.StartPoint, line.Direction);
                if (projectedPoint == line.StartPoint) // Line lies in the plane
                {
                    throw new InvalidOperationException("Line lies in the plane"); // Not sure what should be done here
                }
                else
                {
                    // Line and plane are parallel
                    return null;
                }
            }

            var d = this.SignedDistanceTo(line.StartPoint);
            var u = line.StartPoint.VectorTo(line.EndPoint);
            var t = -1 * d / u.DotProduct(this.Normal);
            if (t > 1 || t < 0)
            {
                // They are not intersected
                return null;
            }

            return line.StartPoint + (t * u);
        }

        /// <summary>
        /// http://www.cs.princeton.edu/courses/archive/fall00/cs426/lectures/raycast/sld017.htm
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public Point3D IntersectionWith(Ray3D ray, double tolerance = float.Epsilon)
        {
            var d = this.SignedDistanceTo(ray.ThroughPoint);
            var t = -1 * d / ray.Direction.DotProduct(this.Normal);
            return ray.ThroughPoint + (t * ray.Direction);
        }

        public Point3D MirrorAbout(Point3D p)
        {
            var p2 = this.Project(p);
            var d = this.SignedDistanceTo(p);
            return p2 - (1 * d * this.Normal);
        }

        public Plane Rotate(UnitVector3D aboutVector, Angle angle)
        {
            var rootPoint = this.RootPoint;
            var rotatedPoint = rootPoint.Rotate(aboutVector, angle);
            var rotatedPlaneVector = this.Normal.Rotate(aboutVector, angle);
            return new Plane(rotatedPlaneVector, rotatedPoint);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(Plane other)
        {
            return this.RootPoint == other.RootPoint && this.Normal == other.Normal;
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            return obj is Plane && this.Equals((Plane)obj);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            unchecked
            {
                var result = this.A.GetHashCode();
                result = (result * 397) ^ this.C.GetHashCode();
                result = (result * 397) ^ this.B.GetHashCode();
                result = (result * 397) ^ this.D.GetHashCode();
                return result;
            }
        }

        public static Point3D PointFromPlanes(Plane plane1, Plane plane2, Plane plane3)
        {
            return Point3D.IntersectionOf(plane1, plane2, plane3);
        }

        public override string ToString()
        {
            return string.Format("A:{0} B:{1} C:{2} D:{3}", Math.Round(this.A, 4), Math.Round(this.B, 4), Math.Round(this.C, 4), Math.Round(this.D, 4));
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();
            var e = (XElement)XNode.ReadFrom(reader);
            this = new Plane(
                UnitVector3D.ReadFrom(e.SingleElement("Normal").CreateReader()),
                Point3D.ReadFrom(e.SingleElement("RootPoint").CreateReader()));
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteElement("RootPoint", this.RootPoint);
            writer.WriteElement("Normal", this.Normal);
        }
    }
}
