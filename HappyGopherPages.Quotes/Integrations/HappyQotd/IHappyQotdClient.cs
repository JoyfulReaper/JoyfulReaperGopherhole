/*
 * Happy Gopher Service
 * Copyright (c) 2026 Kyle Givler
 * Licensed under the MIT License.
 */

namespace HappyGopherPages.Quotes.Integrations.HappyQotd;

public interface IHappyQotdClient
{
    Task<HappyQotdQuote?> GetQuoteOfTheDayAsync(CancellationToken cancellationToken = default);
    Task<HappyQotdQuote?> GetRandomQuoteAsync(CancellationToken cancellationToken = default);
}