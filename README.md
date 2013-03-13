# Riemann C# driver

This is the start of a rewrite of the C# driver by Blue Mountain Capital Management,
availble [here](https://github.com/BlueMountainCapital/riemann-csharp).

Documentation and unit tests are coming.

Some goals for this project:

 * Batch sending by queing events
 * Parity with the Java client
 * Sampling rate
 * statsd-like metrics based on [this](http://labs.amara.org/2012-07-16-metrics.html#riemann) blog post
 * Custom error handling and logging through OnError and OnSend events
