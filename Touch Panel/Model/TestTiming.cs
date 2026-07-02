namespace Touch_Panel.Model
{
    /// <summary>
    /// Hằng số thời gian cho quy trình test — chỉnh ở ĐÂY (đổi giá trị rồi build lại).
    /// </summary>
    public static class TestTiming
    {
        /// <summary>
        /// Delay (ms) chờ ổn định SAU KHI hạ connector (ConnectorAllDown), trước khi bắt đầu đo.
        /// Trước đây hardcode 1000. Tăng nếu baseline/tín hiệu chưa kịp ổn định.
        /// </summary>
        public const int ConnectorSettleDelayMs = 1000;
    }
}
