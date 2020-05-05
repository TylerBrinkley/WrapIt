using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Company;
using NUnit.Framework;
using OtherNamespace;

namespace WrapIt.Tests
{
    public class Tests
    {
        private readonly Dictionary<string, MemoryStream> _files = new Dictionary<string, MemoryStream>();

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task Test1()
        {
            var builder = new WrapperBuilder();
            builder.RootTypes.Add(typeof(Derived));
            await builder.BuildAsync(GetWriter);

            var stream = _files[$"Company.IBase"];
            stream.Position = 0;
            var baseInterface = new StreamReader(stream).ReadToEnd();
            Assert.AreEqual(@"using System;
using OtherNamespace;

namespace Company
{
    public partial interface IBase
    {
        string Dog { get; set; }
        DateTime Raccoon { get; }

        void DoStuff(IOther other);
    }
}", baseInterface);

            stream = _files[$"Company.BaseWrapper"];
            stream.Position = 0;
            var baseClass = new StreamReader(stream).ReadToEnd();
            Assert.AreEqual(@"using System;
using OtherNamespace;

namespace Company
{
    public partial class BaseWrapper : IBase
    {
        public static implicit operator BaseWrapper(Company.Base @object) => @object != null ? new BaseWrapper(@object) : null;

        public static implicit operator Company.Base(BaseWrapper @object) => @object?.Object;

        public Company.Base Object { get; private set; }

        public string Dog { get => Object.Dog; set => Object.Dog = value; }

        public DateTime Raccoon => Object.Raccoon;

        public BaseWrapper(Company.Base @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }

        public void DoStuff(OtherWrapper other) => Object.DoStuff(other);

        void IBase.DoStuff(IOther other) => DoStuff((OtherWrapper)other);

        public override bool Equals(object obj) => Object.Equals(obj is BaseWrapper o ? o.Object : obj);
    }
}", baseClass);

            stream = _files[$"Company.IDerived"];
            stream.Position = 0;
            var derivedInterface = new StreamReader(stream).ReadToEnd();
            Assert.AreEqual(@"using System;
using System.Collections.Generic;
using OtherNamespace;

namespace Company
{
    public partial interface IDerived : IBase, IComparable
    {
        IList<IBase> Array { get; set; }
        decimal Bird { set; }
        IOther Cat { get; set; }
        ICollection Collection { get; set; }
        IOther this[int index] { get; }
        List<string> Names { get; set; }
    }
}", derivedInterface);

            stream = _files[$"Company.DerivedWrapper"];
            stream.Position = 0;
            var derivedClass = new StreamReader(stream).ReadToEnd();
            Assert.AreEqual(@"using System;
using System.Collections.Generic;
using OtherNamespace;
using WrapIt.Collections;

namespace Company
{
    public partial sealed class DerivedWrapper : BaseWrapper, IDerived
    {
        public static implicit operator DerivedWrapper(Company.Derived @object) => @object != null ? new DerivedWrapper(@object) : null;

        public static implicit operator Company.Derived(DerivedWrapper @object) => @object?.Object;

        public new Company.Derived Object => (Company.Derived)base.Object;

        public ArrayWrapper<Company.Base, BaseWrapper, IBase> Array { get => Object.Array; set => Object.Array = value?.ToArray(); }

        IList<IBase> IDerived.Array { get => Array; set => Array = ArrayWrapper<Company.Base, BaseWrapper, IBase>.Create(value); }

        public decimal Bird { set => Object.Bird = value; }

        public OtherWrapper Cat { get => Object.Cat; set => Object.Cat = value; }

        IOther IDerived.Cat { get => Cat; set => Cat = (OtherWrapper)value; }

        public CollectionWrapper Collection { get => Object.Collection; set => Object.Collection = value; }

        ICollection IDerived.Collection { get => Collection; set => Collection = (CollectionWrapper)value; }

        public OtherWrapper this[int index] => Object[index];

        IOther IDerived.this[int index] => this[index];

        public List<string> Names { get => Object.Names; set => Object.Names = value; }

        public DerivedWrapper(Company.Derived @object)
            : base(@object)
        {
        }

        int IComparable.CompareTo(object obj) => ((IComparable)Object).CompareTo(obj is DerivedWrapper o ? o.Object : obj);
    }
}", derivedClass);

            stream = _files[$"Company.ICollection"];
            stream.Position = 0;
            var collectionInterface = new StreamReader(stream).ReadToEnd();
            Assert.AreEqual(@"using System;
using System.Collections;
using System.Collections.Generic;

namespace Company
{
    public partial interface ICollection : IEnumerable, IEnumerable<IDerived>
    {
        IDerived this[int index] { get; }

        IDerived Add(string name);
        IDerived Add(string name, DateTime value);
    }
}", collectionInterface);

            stream = _files[$"Company.CollectionWrapper"];
            stream.Position = 0;
            var collectionClass = new StreamReader(stream).ReadToEnd();
            Assert.AreEqual(@"using System;
using System.Collections;
using System.Collections.Generic;

namespace Company
{
    public partial class CollectionWrapper : ICollection
    {
        public static implicit operator CollectionWrapper(Company.Collection @object) => @object != null ? new CollectionWrapper(@object) : null;

        public static implicit operator Company.Collection(CollectionWrapper @object) => @object?.Object;

        public Company.Collection Object { get; private set; }

        public DerivedWrapper this[int index] => Object[index];

        IDerived ICollection.this[int index] => this[index];

        public CollectionWrapper(Company.Collection @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }

        public DerivedWrapper Add(string name) => Object.Add(name);

        IDerived ICollection.Add(string name) => Add(name);

        public DerivedWrapper Add(string name, DateTime value) => Object.Add(name, value);

        IDerived ICollection.Add(string name, DateTime value) => Add(name, value);

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<IDerived>)this).GetEnumerator();

        public IEnumerator<DerivedWrapper> GetEnumerator()
        {
            foreach (var item in Object)
            {
                yield return (Company.Derived)item;
            }
        }

        IEnumerator<IDerived> IEnumerable<IDerived>.GetEnumerator() => GetEnumerator();
    }
}", collectionClass);

            stream = _files[$"OtherNamespace.IOther"];
            stream.Position = 0;
            var otherInterface = new StreamReader(stream).ReadToEnd();
            Assert.AreEqual(@"using System;
using Company;

namespace OtherNamespace
{
    public partial interface IOther
    {
        int? Count { get; set; }
        DateTime? this[string name, int? index] { get; set; }
        string this[IBase b] { set; }
        string[] StringArray { get; set; }

        event FieldChangeEventHandlerWrapper FieldChange;
    }
}", otherInterface);

            stream = _files[$"OtherNamespace.FieldChangeEventHandlerWrapper"];
            stream.Position = 0;
            var fieldChangeEventHandlerWrapper = new StreamReader(stream).ReadToEnd();
            Assert.AreEqual(@"namespace OtherNamespace
{
    public delegate void FieldChangeEventHandlerWrapper(object source, IFieldChangeEventArgs e);
}", fieldChangeEventHandlerWrapper);

            stream = _files[$"OtherNamespace.IFieldChangeEventArgs"];
            stream.Position = 0;
            var fieldChangeEventArgsInterface = new StreamReader(stream).ReadToEnd();
            Assert.AreEqual(@"namespace OtherNamespace
{
    public partial interface IFieldChangeEventArgs
    {
        int BorrowerPair { get; }
        string FieldId { get; }
        string NewValue { get; }
        string PriorValue { get; }
    }
}", fieldChangeEventArgsInterface);

            stream = _files[$"OtherNamespace.FieldChangeEventArgsWrapper"];
            stream.Position = 0;
            var fieldChangeEventArgsClass = new StreamReader(stream).ReadToEnd();
            Assert.AreEqual(@"using System;

namespace OtherNamespace
{
    public partial class FieldChangeEventArgsWrapper : IFieldChangeEventArgs
    {
        public static implicit operator FieldChangeEventArgsWrapper(OtherNamespace.FieldChangeEventArgs @object) => @object != null ? new FieldChangeEventArgsWrapper(@object) : null;

        public static implicit operator OtherNamespace.FieldChangeEventArgs(FieldChangeEventArgsWrapper @object) => @object?.Object;

        public OtherNamespace.FieldChangeEventArgs Object { get; private set; }

        public int BorrowerPair => Object.BorrowerPair;

        public string FieldId => Object.FieldId;

        public string NewValue => Object.NewValue;

        public string PriorValue => Object.PriorValue;

        public FieldChangeEventArgsWrapper(OtherNamespace.FieldChangeEventArgs @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }
    }
}", fieldChangeEventArgsClass);

            stream = _files[$"OtherNamespace.OtherWrapper"];
            stream.Position = 0;
            var otherClass = new StreamReader(stream).ReadToEnd();
            Assert.AreEqual(@"using System;
using Company;

namespace OtherNamespace
{
    public partial class OtherWrapper : IOther
    {
        public static implicit operator OtherWrapper(OtherNamespace.Other @object) => @object != null ? new OtherWrapper(@object) : null;

        public static implicit operator OtherNamespace.Other(OtherWrapper @object) => @object?.Object;

        public OtherNamespace.Other Object { get; private set; }

        public int? Count { get => Object.Count; set => Object.Count = value; }

        public DateTime? this[string name, int? index] { get => Object[name, index]; set => Object[name, index] = value; }

        public string this[BaseWrapper b] { set => Object[b] = value; }

        string IOther.this[IBase b] { set => this[(BaseWrapper)b] = value; }

        public string[] StringArray { get => Object.StringArray; set => Object.StringArray = value; }

        public event FieldChangeEventHandlerWrapper FieldChange
        {
            add => AddOrRemoveFieldChange(value, true);
            remove => AddOrRemoveFieldChange(value, false);
        }

        private void AddOrRemoveFieldChange(FieldChangeEventHandlerWrapper value, bool toAdd)
        {
            if (value != null)
            {
                OtherNamespace.FieldChangeEventHandler handler = (source, e) => value(TryWrap(source), (FieldChangeEventArgsWrapper)e);
                if (toAdd)
                {
                    Object.FieldChange += handler;
                }
                else
                {
                    Object.FieldChange -= handler;
                }
            }
        }

        protected virtual object TryWrap(object obj) => obj is OtherNamespace.Other o ? (OtherWrapper)o : obj;

        public OtherWrapper(OtherNamespace.Other @object)
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
        }
    }
}", otherClass);
        }

        private Task<TextWriter> GetWriter(Type type, string classFullName, CancellationToken cancellationToken)
        {
            var stream = new MemoryStream();
            TextWriter writer = new StreamWriter(stream, Encoding.UTF8, 1024, leaveOpen: true);
            _files.Add(classFullName, stream);
            return Task.FromResult(writer);
        }
    }
}

namespace Company
{
    public class Base
    {
        private readonly DateTime _raccoon = DateTime.Now;

        public string Dog { get; set; }

        public DateTime Raccoon => _raccoon;

        public virtual void DoStuff(Other other)
        {
        }

        public override bool Equals(object obj) => base.Equals(obj);
    }

    public sealed class Derived : Base, IComparable
    {
        private decimal _bird;

        public Base[] Array { get; set; }

        public decimal Bird { set => _bird = value; }

        public Other Cat { get; set; }

        public Collection Collection { get; set; }

        public Other this[int index] => Cat;

        public List<string> Names { get; set; }

        public override void DoStuff(Other other)
        {
        }

        int IComparable.CompareTo(object obj) => 1;
    }

    public class Collection : IEnumerable
    {
        public Derived this[int index] => null;

        public Derived Add(string name) => null;

        public Derived Add(string name, DateTime value) => null;

        public IEnumerator GetEnumerator() => null;
    }
}

namespace OtherNamespace
{
    public class Other
    {
        public DateTime? this[string name, int? index]
        {
            get => null;
            set { }
        }

        public string this[Base b] { set { } }

        public int? Count { get; set; }

        public string[] StringArray { get; set; }

        public event FieldChangeEventHandler FieldChange;
    }

    public delegate void FieldChangeEventHandler(object source, FieldChangeEventArgs e);

    public class FieldChangeEventArgs : EventArgs
    {
        public int BorrowerPair { get; }

        public string FieldId { get; }

        public string NewValue { get; }

        public string PriorValue { get; }
    }
}