using GestureSign.InputRecorder.Recording;
using GestureSign.Tests.Hid;
using GestureSign.Tests.Replay;
using System;
using System.Linq;
using Xunit;

namespace GestureSign.Tests.Recording
{
    /// <summary>
    /// The recorder's live contact view and its "stop when released" take detection rely on
    /// <see cref="ContactDecoder"/> + <see cref="ContactTracker"/> seeing exactly the contacts that are down.
    /// </summary>
    public class ContactTrackerTests
    {
        private const ushort DigitizerPage = 0x0D, GenericDesktopPage = 0x01;
        private const ushort TipSwitch = 0x42, InRange = 0x32, ContactId = 0x51, ContactCount = 0x54, X = 0x30, Y = 0x31;

        private static readonly Lazy<byte[]> TouchpadPp = new Lazy<byte[]>(() => HidPreparsedDataBuilder.Build(HidDescriptors.PrecisionTouchpad));
        private static readonly Lazy<byte[]> TouchScreenPp = new Lazy<byte[]>(() => HidPreparsedDataBuilder.Build(HidDescriptors.MultiTouchScreen));
        private static readonly Lazy<byte[]> PenPp = new Lazy<byte[]>(() => HidPreparsedDataBuilder.Build(HidDescriptors.Pen));

        private sealed class Finger
        {
            public int Id;
            public int X;
            public int Y;
            public bool Tip = true;
        }

        private static byte[] TouchpadReport(int contactCount, params Finger[] slots)
        {
            var report = HidReport.Create(TouchpadPp.Value, HidDescriptors.TouchpadReportId)
                .SetValue(DigitizerPage, 0, ContactCount, contactCount);
            for (int i = 0; i < slots.Length; i++)
            {
                short link = (short)(i + 1);
                report.SetValue(DigitizerPage, link, ContactId, slots[i].Id)
                    .SetValue(GenericDesktopPage, link, X, slots[i].X)
                    .SetValue(GenericDesktopPage, link, Y, slots[i].Y);
                if (slots[i].Tip)
                    report.SetButtons(DigitizerPage, link, TipSwitch);
            }
            return report.ToArray();
        }

        private static void Apply(ContactDecoder decoder, ContactTracker tracker, byte[] report)
        {
            DecodedReport decoded = decoder.Decode(report, 0, report.Length);
            Assert.NotNull(decoded);
            tracker.Apply(decoded);
        }

        [Fact]
        public void TouchpadSlotsBeyondTheContactCountAreIgnored()
        {
            using (ContactDecoder decoder = ContactDecoder.TryCreate(TouchpadPp.Value))
            {
                var tracker = new ContactTracker(pen: false);
                // Slot 3 holds stale data with its tip still set; Contact Count says only 2 slots are valid.
                Apply(decoder, tracker, TouchpadReport(2,
                    new Finger { Id = 0, X = 1024, Y = 4095 },
                    new Finger { Id = 1, X = 3071, Y = 0 },
                    new Finger { Id = 2, X = 2000, Y = 2000 }));

                Assert.Equal(new[] { 0, 1 }, tracker.Active.Select(s => s.ContactId).OrderBy(id => id));
                var first = tracker.Strokes[0].Points[0];
                Assert.Equal(0.25, first.X, 2);
                Assert.Equal(1.0, first.Y, 2);
            }
        }

        [Fact]
        public void TipOffReportsLiftTheContacts()
        {
            using (ContactDecoder decoder = ContactDecoder.TryCreate(TouchpadPp.Value))
            {
                var tracker = new ContactTracker(pen: false);
                Apply(decoder, tracker, TouchpadReport(3, new Finger { Id = 0 }, new Finger { Id = 1 }, new Finger { Id = 2 }));
                Apply(decoder, tracker, TouchpadReport(3, new Finger { Id = 0, Tip = false }, new Finger { Id = 1, Tip = false }, new Finger { Id = 2, Tip = false }));

                Assert.Equal(0, tracker.ActiveCount);
                Assert.Equal(3, tracker.MaxActive);
                Assert.Equal(3, tracker.Strokes.Count);
                Assert.All(tracker.Strokes, s => Assert.True(s.Ended));
            }
        }

        [Fact]
        public void ContactMissingFromACompleteFrameIsLifted()
        {
            using (ContactDecoder decoder = ContactDecoder.TryCreate(TouchpadPp.Value))
            {
                var tracker = new ContactTracker(pen: false);
                Apply(decoder, tracker, TouchpadReport(2, new Finger { Id = 0 }, new Finger { Id = 1 }));
                // The device dropped the lift report of contact 1.
                Apply(decoder, tracker, TouchpadReport(1, new Finger { Id = 0 }));

                Assert.Equal(new[] { 0 }, tracker.Active.Select(s => s.ContactId));
                Assert.True(tracker.Strokes.Single(s => s.ContactId == 1).Ended);
            }
        }

        [Fact]
        public void ReusedContactIdAfterLiftStartsANewStroke()
        {
            using (ContactDecoder decoder = ContactDecoder.TryCreate(TouchpadPp.Value))
            {
                var tracker = new ContactTracker(pen: false);
                Apply(decoder, tracker, TouchpadReport(1, new Finger { Id = 0, X = 100 }));
                Apply(decoder, tracker, TouchpadReport(1, new Finger { Id = 0, X = 200, Tip = false }));
                Apply(decoder, tracker, TouchpadReport(1, new Finger { Id = 0, X = 300 }));

                Assert.Equal(2, tracker.Strokes.Count);
                Assert.True(tracker.Strokes[0].Ended);
                Assert.False(tracker.Strokes[1].Ended);
                Assert.Equal(1, tracker.ActiveCount);
            }
        }

        [Fact]
        public void HybridTouchScreenFrameSpansReports()
        {
            byte[] pp = TouchScreenPp.Value;
            Func<int, int[], byte[]> report = (count, ids) =>
            {
                var r = HidReport.Create(pp, HidDescriptors.TouchScreenReportId).SetValue(DigitizerPage, 0, ContactCount, count);
                for (int i = 0; i < ids.Length; i++)
                    r.SetValue(DigitizerPage, (short)(i + 1), ContactId, ids[i]).SetButtons(DigitizerPage, (short)(i + 1), TipSwitch);
                return r.ToArray();
            };

            using (ContactDecoder decoder = ContactDecoder.TryCreate(pp))
            {
                var tracker = new ContactTracker(pen: false);
                Apply(decoder, tracker, report(3, new[] { 10, 11 }));
                Assert.Equal(2, tracker.ActiveCount);

                // Follow-up report: Contact Count 0, one valid slot. Its second slot is unused.
                Apply(decoder, tracker, report(0, new[] { 12, 99 }));
                Assert.Equal(new[] { 10, 11, 12 }, tracker.Active.Select(s => s.ContactId).OrderBy(id => id));

                // Next frame starts with the same three contacts; nothing may be lifted mid-frame.
                Apply(decoder, tracker, report(3, new[] { 10, 11 }));
                Assert.Equal(3, tracker.ActiveCount);
            }
        }

        [Fact]
        public void HoveringPenIsActiveUntilItLeavesRange()
        {
            byte[] pp = PenPp.Value;
            Func<ushort[], byte[]> report = buttons => HidReport.Create(pp, HidDescriptors.PenReportId)
                .SetValue(GenericDesktopPage, 0, X, 10).SetValue(GenericDesktopPage, 0, Y, 10)
                .SetButtons(DigitizerPage, 0, buttons).ToArray();

            using (ContactDecoder decoder = ContactDecoder.TryCreate(pp))
            {
                Assert.True(decoder.IsPen);
                var tracker = new ContactTracker(decoder.IsPen);
                Apply(decoder, tracker, report(new[] { InRange }));
                Assert.Equal(1, tracker.ActiveCount);
                Apply(decoder, tracker, report(new ushort[0]));
                Assert.Equal(0, tracker.ActiveCount);
            }
        }

        [Fact]
        public void TouchpadPhysicalSizeComesFromTheDescriptorUnits()
        {
            using (ContactDecoder decoder = ContactDecoder.TryCreate(TouchpadPp.Value))
            {
                Assert.Equal(HidDescriptors.TouchpadFingers, decoder.ContactSlots);
                // 4.00" x 2.75" (inch, exponent -2).
                Assert.InRange(decoder.PhysicalSizeMm.Value.Width, 101.55f, 101.65f);
                Assert.InRange(decoder.PhysicalSizeMm.Value.Height, 69.80f, 69.90f);
            }
        }

        [Theory]
        [InlineData("touchpad-3finger-left", 3)]
        [InlineData("touchpad-2finger-down", 2)]
        [InlineData("touchscreen-3finger-down-hybrid-split", 3)]
        public void SyntheticRecordingsDecodeToTheirFingerCountAndEndReleased(string name, int fingers)
        {
            InputRecording recording = SyntheticRecordings.All().Single(r => r.Key == name).Value;
            RecordedDevice device = recording.Devices.Single();
            using (ContactDecoder decoder = ContactDecoder.TryCreate(Convert.FromBase64String(device.PreparsedData)))
            {
                var tracker = new ContactTracker(decoder.IsPen);
                foreach (RecordedEvent e in recording.Events.Where(e => e.Type == RecordedEventTypes.Hid))
                {
                    byte[] data = Convert.FromBase64String(e.Data);
                    for (int i = 0; i < e.Count; i++)
                        tracker.Apply(decoder.Decode(data, i * e.SizeHid, e.SizeHid));
                }
                Assert.Equal(fingers, tracker.MaxActive);
                Assert.Equal(0, tracker.ActiveCount);
            }
        }
    }
}
