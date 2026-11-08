/*
 * Happy Gopher Service
 * Copyright (c) 2026 Kyle Givler
 * Licensed under the MIT License.
 */

namespace HappyGopherPages.Quotes.Integrations.HappyQotd;

public sealed record HappyQotdQuote(
    long Id,
    string Text,
    string? Author = null,
    string? Source = null);