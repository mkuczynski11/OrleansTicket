using Orleans.Concurrency;

namespace OrleansTicket.Actors
{
    public interface IExchangeCurrencyGrain : IGrainWithGuidKey
    {
        Task<double> Exchange(double amount, string fromCurrency);
    }
    /// <summary>
    /// Grain representing Currency exchange worker.
    /// ExchangeCurrency is a worker without a state with the purpose of exchanging currencies.
    /// </summary>
    [StatelessWorker(maxLocalWorkers: 3)]
    public class ExchangeCurrency : Grain, IExchangeCurrencyGrain
    {
        private static bool ShouldDelay = true;
        private double CurrencyRate(string currency)
        {
            switch (currency)
            {
                case "":
                    return 1;
                case "EUR":
                    return 0.23;
                case "USD":
                    return 0.25;
                default:
                    throw new ArgumentException();
            }
        }
        public async Task<double> Exchange(double amount, string fromCurrency)
        {
            if (ShouldDelay)
            {
                Console.WriteLine("Creating delay of 5 seconds");
                await Task.Delay(5000);
            }
            return amount * CurrencyRate(fromCurrency);
        }
    }
}
