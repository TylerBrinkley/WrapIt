using NUnit.Framework;
using Company.OtherNamespace;
using Wrappers.OtherNamespace;

namespace WrapIt.Tests
{
    public class EventTests
    {
        private bool _didStuff;

        [Test]
        public void AddingAndRemovingEventHandlers()
        {
            var other = new Other();
            var otherWrapper = new OtherWrapper(other);
            other.InvokeFieldChange();
            Assert.False(_didStuff);
            otherWrapper.FieldChange += OtherWrapper_FieldChange;
            other.InvokeFieldChange();
            Assert.True(_didStuff);
            _didStuff = false;
            otherWrapper.FieldChange -= OtherWrapper_FieldChange;
            other.InvokeFieldChange();
            Assert.False(_didStuff);
        }

        private void OtherWrapper_FieldChange(object source, IFieldChangeEventArgs e)
        {
            _didStuff = true;
        }
    }
}
