# 4Has4Letters-Georgian

 This was written to find the largest sequence of numbers where the next number is the amount of characters the previous one has in the alphabetical representation: <https://youtu.be/LYKn0yUTIU4>

## Commandline Arguments

| Arguments      | Description                                                       |
| -------------- | ----------------------------------------------------------------- |
| --help         | Print help                                                        |
| -t, --threads  | Number of CPU threads list                                        |
| -g, --gpu      | Use GPU instead of the CPU                                        |
| -c, --cuda     | Use CUDA for GPU. Otherwise, OpenCL will be used                  |
| -b, --blocks   | The maximum number of CUDA blocks. 100,000 by default             |
| -m, --memory   | The maximum System Memory (GB) should OpenCL use (8 by default)   |
| -s, --start    | The start of the range of numbers to test. 0 by default           |
| -e, --end      | The end of the range of numbers to test. 1,000,000,000 by default |
| -o, --output   | The output file path. If whitespace, Console will be used instead |
| -n, --no-comma | Don't use comma separators in Georgian number representation      |
| --version      | Display version information                                       |

### Note

The code can be run on the **CPU**, or **GPU** with either **CUDA** or **OpenCL**.

If my testing, running using **OpenCL** was the fastest,
though it's probably because the **CUDA** code is not well written.
This was the first time I tried writing **GPU** accelerated code.

Beware, that if you do use **OpenCL**, the code will preallocate
System Memory blocks of around your GPU memory size *for every thread*,
so I reccomend you also set the `-m, --memory` argument to -6 GB orso of
your total System Memory, otherwise, it will start to use your Virtual Memory,
or ever fail the execution if the memory argument is set too high!

## License

MIT License

Copyright (c) 2021 Guiorgy

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
