using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuttleMate.ModelViews.DashboardModelViews
{
    public class TransactionStatisticsModel
    {
        public int TotalTransaction { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PaidTransaction { get; set; }
        public int UnpaidTransaction { get; set; }
        public int RefundedTransaction { get; set; }
        public List<TransactionChartData> TransactionChart { get; set; } = new();
    }

    public class TransactionChartData
    {
        public DateTime Date { get; set; }
        public int TransactionCount { get; set; }
        public decimal Revenue { get; set; }
    }
}
