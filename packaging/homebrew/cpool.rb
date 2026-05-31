class Cpool < Formula
  desc "Card pool generator for trading card game formats"
  homepage "https://github.com/piers-sinclair/cardpool"
  version "1.2.1"
  license "MIT"

  on_macos do
    if Hardware::CPU.arm?
      url "https://github.com/piers-sinclair/cardpool/releases/download/v#{version}/cpool-osx-arm64.zip"
      sha256 "0000000000000000000000000000000000000000000000000000000000000000"
    else
      url "https://github.com/piers-sinclair/cardpool/releases/download/v#{version}/cpool-osx-x64.zip"
      sha256 "0000000000000000000000000000000000000000000000000000000000000000"
    end
  end

  on_linux do
    url "https://github.com/piers-sinclair/cardpool/releases/download/v#{version}/cpool-linux-x64.zip"
    sha256 "0000000000000000000000000000000000000000000000000000000000000000"
  end

  def install
    bin.install "cpool"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cpool --version")
  end
end
