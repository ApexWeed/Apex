using System;

namespace Apex.Layout
{
    public struct LayoutEndStub : IDisposable
    {
        public enum StubType
        {
            Row,
            GroupBox,
            Panel
        }

        private LayoutEngine LayoutEngine;
        private StubType EndStubType;

        public LayoutEndStub(LayoutEngine LayoutEngine, StubType StubType)
        {
            this.LayoutEngine = LayoutEngine;
            this.EndStubType = StubType;
        }

        public void Dispose()
        {
            switch (EndStubType)
            {
                case StubType.Row:
                    {
                        LayoutEngine.EndRow();
                        break;
                    }
                case StubType.GroupBox:
                    {
                        LayoutEngine.EndGroupBox();
                        break;
                    }
                case StubType.Panel:
                    {
                        LayoutEngine.EndPanel();
                        break;
                    }
                default:
                    {
                        break;
                    }
            }

        }
    }
}
