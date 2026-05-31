class Cpool < Formula
  desc "Card pool generator for trading card games, originally built for the 25 Format"
  homepage "https://github.com/piers-sinclair/cardpool"
  version "1.2.2"
  license "MIT"

  if OS.mac? && Hardware::CPU.arm?
    url "https://github.com/piers-sinclair/cardpool/releases/download/v#{version}/cpool-osx-arm64.zip"
    sha256 "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
  elsif OS.mac?
    url "https://github.com/piers-sinclair/cardpool/releases/download/v#{version}/cpool-osx-x64.zip"
    sha256 "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
  else
    url "https://github.com/piers-sinclair/cardpool/releases/download/v#{version}/cpool-linux-x64.zip"
    sha256 "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"
  end

  def install
    bin.install "cpool"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cpool --version")
  end
end
