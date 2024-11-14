# BaselineAnalyzer

C#初心者に優しいアナライザーパッケージ

![BaselineAnalyzer](Images/BaselineAnalyzer.100.png)

# Status

[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](https://www.repostatus.org/badges/latest/wip.svg)](https://www.repostatus.org/#wip)

|Target|Pakcage|
|:----|:----|
|Any|[![NuGet BaselineAnalyzer](https://img.shields.io/nuget/v/BaselineAnalyzer.svg?style=flat)](https://www.nuget.org/packages/BaselineAnalyzer)|


----

[English language is here](https://github.com/kekyo/BaselineAnalyzer)

## これは何ですか?

これは、.NET C#初心者向けの、些細な実装ミスを自動的に検出してくれるアナライザーです。
使い方は簡単で、あなたのプロジェクトに [BaselineAnalyzer NuGetパッケージ](https://www.nuget.org/packages/BaselineAnalyzer) をインストールするだけです。

VisualStudio、Rider、またはcsprojファイルを直接編集して、 `BaselineAnalyzer` パッケージをインストールしてください。
これで、IDEやコンパイラが、ソースコード中の問題を検出して報告するようになります。

アナライザーは色々なものがリリースされていますが、このパッケージは特にC#初心者が陥りがちな問題に絞って検出を行います。
パッケージの設定等も不要で、簡単に使用できます。


----

## 検出可能なソースコードの問題点

BaselineAnalyzerは、IDEやコンパイラが問題を検出すると、 `BLA0001` のような識別コードとメッセージを出力します。
IDEの場合はメッセージウインドウに、コンパイル中に発見した場合はコンパイラのメッセージとして出力されます。

以下に、識別子に対応する問題点の一覧を示します。

|識別子|概要|
|:----|:----|
|`BLA0001`|例外を `catch` した際に、例外をスローすることなく `catch` ブロックを抜けるような実装がある|
|`BLA0002`|例外を再スローする際に、 `throw` 句を使わずに `throw ex` のように、元の例外オブジェクトを指定|
|`BLA0011`|非同期メソッドを実装する場合に、メソッドの名の末尾に `Async` と付けていない|
|`BLA0012`|同期メソッドを実装する場合に、メソッドの名の末尾に `Async` と付けている|
|`BLA0021`|非同期メソッドの実装内で、`Task.Wait()` メソッドの呼び出しや `Task.Result` プロパティを参照している|


### BLA0001: The catch block does not contain a throw statement

これは、例外を `catch` した際に、例外をスローすることなく `catch` ブロックを抜けるようなコードを記述すると発生します:

```csharp
public void Foo()
{
    try
    {
        // なにかする...
    }
    catch (Exception ex)
    {
        Console.WriteLine("Caught an exception!");

        // 例外をスローしない...
    }
}
```

このようなコードは、例外が含む危険な状況を無視する可能性があります。
問題を修正する例を示します:

```csharp
public void Foo()
{
    try
    {
        // なにかする...
    }
    catch (Exception ex)
    {
        Console.WriteLine("Caught an exception!");
        
        // 例外を再スローする
        throw;
    }
}
```

上記のように、 `catch` ブロック内で例外がスローされれば問題はありません。
或いは、別の例外にラップしてスローする事が考えられます:

```csharp
public void Foo()
{
    try
    {
        // なにかする...
    }
    catch (Exception ex)
    {
        Console.WriteLine("Caught an exception!");
        
        // 例外をラップしてスローする
        throw new FormatException("Invalid form", ex);
    }
}
```

#### なぜですか？

例外には、例外的な事象、つまり正常には発生しない異常な状況が発生した事を伝える役割があります。
`catch` ブロックでスローしないと言う事は、異常な状態を「無視」したことに繋がる可能性があり、もっと大きな問題に気が付けない可能性があります。
そのような状況を早期に発見するためにも、例外を握りつぶさない方が良いのです。

### BLA0002: Avoid using 'throw ex;' in catch blocks, use 'throw;' instead to preserve stack trace

これは、例外を再スローする際に、 `throw` 句を使わずに `throw ex` のように、元の例外オブジェクトを指定した場合に発生します:

```csharp
public void Foo()
{
    try
    {
        // なにかする...
    }
    catch (Exception ex)
    {
        Console.WriteLine("Caught an exception!");

        // 元の例外オブジェクトを指定してスローする...
        throw ex;
    }
}
```

一見すると問題ないように見えますが、このようにスローすると、例外発生元を示すスタックトレース情報が失われ、例外は「ここ」で発生したかのように扱われます。
その結果、例外をログに記録した場合に、ログ内に正しくないスタックトレースが格納されたり、デバッガで例外の正しい発生個所を追跡できなくなったりします。

例外発生時に、何か特別な処理を行いたいだけである場合は、例外を「再スロー」させます。再スローは、特別な構文 `throw` 句で実現します:

```csharp
public void Foo()
{
    try
    {
        // なにかする...
    }
    catch (Exception ex)
    {
        Console.WriteLine("Caught an exception!");

        // 元の例外オブジェクトを「再スロー」する
        throw;
    }
}
```

### BLA0011: Asynchronous method 'Foo' must include the 'Async' suffix

非同期メソッドを実装する場合は、メソッドの名の末尾に `Async` と付けることが慣例となっています:

```csharp
// 非同期メソッドにも関わらず、メソッド名末尾に `Async` がついていない
public Task Foo()
{
    // ...
}
```

メソッド名の末尾に `Async` と付けることで解消できます:

```csharp
// 非同期メソッドなので、メソッド名末尾に `Async` とついている
public Task FooAsync()
{
    // ...
}
```

非同期メソッドかどうかは、以下の条件で検出します:

* `Task` 又は `ValueTask` とそのジェネリックバージョンの型を返却するメソッド。
  * 但し、`Task` の場合の `static Main` メソッドを除く。
* `IAsyncEnumerable<T>` の型を返却するメソッド。
  * 但し、拡張メソッドを除く（`System.Linq.Async` のような非同期演算子を想定）。

#### なぜですか?

理由はいくつか考えられますが、一つは、C#に `async-await` が導入された当初からの慣例だからです。
`async-await` が導入された当初、従来の同期メソッドの非同期バージョンを追加するのに、オーバーロードが使えず、区別可能にするための命名規約として使用されました。
例えば、`Stream`クラスには`Read`メソッドがありますが、この非同期バージョンはメソッド名を変える必要があるのと、非同期メソッドであることが明確となる利点がありました:

```csharp
// .NET標準のStreamクラス
public class Stream
{
    // ...
    
    // ストリームからデータを読み取る（同期バージョン）
    public abstract int Read(byte[] buffer, int offset, int count);

    // ストリームからデータを読み取る（追加された非同期バージョン）
    public abstract Task<int> ReadAsync(byte[] buffer, int offset, int count);
}
```

上記のように、 `Task<int>` を返却するメソッドを追加しようとしても、オーバーロードの規則で追加できません（同じ引数シグネチャを持つオーバーロードは複数定義できない）。

### BLA0012: Synchronous method 'FooAsync' must not include the 'Async' suffix

同期メソッドを実装する場合は、メソッドの名の末尾に `Async` と付けると誤解を生みます:

```csharp
// 同期メソッドにも関わらず、メソッド名末尾に `Async` がついている
public int FooAsync()
{
    // ...
}
```

メソッド名の末尾から `Async` を外すと解消できます:

```csharp
// 同期メソッドなので、メソッド名末尾に `Async` とついていない
public int Foo()
{
    // ...
}
```

### なぜですか?

これは `BLA0011` の逆のパターンです。詳しくは `BLA0011` を参照してください。

### BLA0021: Using 'Wait' in asynchronous methods may cause deadlock

非同期メソッドの実装内で、`Task.Wait()` メソッドの呼び出しや `Task.Result` プロパティを参照しています:

```csharp
public string Foo(HttpClient httpClient)
{
    // 非同期メソッドを呼び出して、 `Result` で同期的に待機している
    var result = httpClient.GetStringAsync("https://example.com/").Result;
}
```

非同期メソッドの結果は、必ず `async-await` 句を使用して、「非同期的」に待機してください:

```csharp
public async Task<string> FooAsync(HttpClient httpClient)
{
    // `Result` を使わずに `await` 句を使用して、非同期的に待機する
    var result = await httpClient.GetStringAsync("https://example.com/");
}
```

### なぜですか?

`Task.Wait()` や `Task.Result` を使用すると、スレッドがそこで強制的に一時停止させられます。その結果:

* GUIアプリケーションの場合は、通常はメインスレッドのみを使用して動作しているので、非同期メソッドが終了するまでアプリケーション全体が停止します。
* サーバサイドアプリケーションの場合は、スレッドの再利用が出来なくなり、同時実行時のパフォーマンスの悪化に繋がります。
* コードが入り組んでいる場合、もしかしたらデッドロックして、アプリケーションを強制終了するまで動かないかもしれません。

上記のどの問題も、私のコードには関係ない、と思うかもしれません。
しかし、もう一度修正方法を見て下さい。正しいコードを記述するのはそれほど大変な事ではありません。
もし、非同期メソッドの推奨される実装パターンを無視して実装を続けた場合、後から大量のコードを修正するのは相当な苦労を伴う事を覚悟しておいてください。


----

## メッセージの無効化

私たちの世界に「絶対」と言う事がないのと同じで、これらの問題が問題ではない、と確信できる場合は、[`#pragma warning`](https://learn.microsoft.com/ja-jp/dotnet/fundamentals/code-analysis/suppress-warnings#use-a-preprocessor-directive) を使って、メッセージを無効化出来ます。

しかし、何が問題なのか分かっていないのであれば、無効化する前に指摘箇所を良く理解して、問題のあるコードを修正しましょう。
指摘箇所を理解しないまま無効化した場合、恐らくはろくな結果を生まないか、将来その問題があなたを襲うでしょう。

警告しましたよ？ :)


## 制限

このアナライザーは、非常に細かい実装の詳細までは解析しません。従って、本来問題ないと考えらる実装であっても警告を発生したり、その逆が起こり得ます。


## TODO

* いくつかのルールを追加する。


----

## License

Apache-v2

## History

* 0.1.0:
  * 初版
