namespace Wrappers
{
    public sealed partial class PointWrapper : IPoint
    {
        /// <summary>
        /// The conversion operator for wrapping the <see cref="Company.Point"/> object.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public static implicit operator PointWrapper(Company.Point @object) => new PointWrapper(@object);

        /// <summary>
        /// The conversion operator for unwrapping the <see cref="Company.Point"/> object.
        /// </summary>
        /// <param name="object">The object to unwrap.</param>
        public static implicit operator Company.Point(PointWrapper @object) => @object?.Object ?? default;

        /// <summary>
        /// The wrapped object.
        /// </summary>
        public Company.Point Object { get; set; }

        public int X { get => Object.X; set { var @object = Object; @object.X = value; Object = @object; } }

        public int Y { get => Object.Y; set { var @object = Object; @object.Y = value; Object = @object; } }

        /// <summary>
        /// The wrapper constructor.
        /// </summary>
        /// <param name="object">The object to wrap.</param>
        public PointWrapper(Company.Point @object)
        {
            Object = @object;
        }

        public PointWrapper()
            : this(new Company.Point())
        {
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Object.Equals(obj is PointWrapper o ? o.Object : obj);

        /// <inheritdoc/>
        public override int GetHashCode() => Object.GetHashCode();

        /// <inheritdoc/>
        public override string ToString() => Object.ToString();
    }
}