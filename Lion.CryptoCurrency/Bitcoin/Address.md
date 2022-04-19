## Legacy address - P2PKH – Pay To Public Key Hash
---
01、生成64位随机数 <B>[ 私钥 HEX ]</B><br>
ee2af874fff0fb0d096fc9f1812609de4f0ce5ae3999840209b91a95f12e6035<br>
<br>
02、HEX转换成数字<br>
107726380326939678096191321759894641171889680277176394980912448220763383554101<br>
<br>
03、Secp256k1计算(02) <B>[ 公钥 ]</B><br>
04b713a951cca5cdac6cdade5a8147df3c219429b71ba78fb096e19e65b6c027ee4876eb9db0be52bc43c6381efb6b3e00a1473f491e778d07eb1ab2f7712f09ad<br>
<br>
04、SHA256计算公钥得到<br>
1cb42f2dc7b8d6ae929f36601939eda86e7cf12889cf7408f4790c71111be6ce<br>
<br>
05、RIPEMD160计算(04)的结果得到<br>
396295cce47caab7d728bf8612dd3c86e0c0c186<br>
<br>
06、(05)的结果前加上版本0x00 (testnet 加 0x6f)<br>
00396295cce47caab7d728bf8612dd3c86e0c0c186<br>
<br>
07、SHA256计算(06)的结果得到<br>
8f89a1b149963173285db1aab294fddbf613c97a8d1dfaed561263a2ba8d56b7<br>
<br>
08、SHA256计算(07)的结果得到<br>
885635c71f6b0941d86b6fdb4587053e568bc7886a78cbffbffef925414341fa<br>
<br>
09、拼接(06)的结果和(08)的结果的前4个Byte<br>
00396295cce47caab7d728bf8612dd3c86e0c0c186885635c7<br>
<br>
10、私钥前加上版本0x80（testnet 加 0xef)<br>
80ee2af874fff0fb0d096fc9f1812609de4f0ce5ae3999840209b91a95f12e6035
<br>
11、SHA256计算(10)的结果得到<br>
128dbba1d774b53c7b484ca64abee7f68240591bc45ae3c83b366a307ed05102<br>
<br>
12、SHA256计算(11)的结果得到<br>
c04ece8d25b157e01e33026351448c742447872f8ed56af5d830fec54cfc3ca0<br>
<br>
13、合并(10)和(12)的前4个Byte<br>
c04ece8d25b157e01e33026351448c742447872f8ed56af5d830fec54cfc3ca0c04ece8d<br>
<br>
14、BASE58计算(09)得到 <B>[ 地址 ]</B><br>
16ERckZwzGreT9bB3wMRWZnBsW7iHzKLBx<br>
<br>
15、BASE58计算(13)得到 <B>[ 私钥 WIF ]</B><br>
5KdBG1oWfPmsS8AGRmXTdBuzrmignK5KXjKczTU1DQH9crrxEy2<br>
<br>
<br>

---
## Legacy Bech32 address
---

---
## SegWit Address - P2SH - Pay To Script Hash
---
01、生成64位随机数(最多6个私钥HEX) <B>[ 私钥 HEX ]</B><br>
ee2af874fff0fb0d096fc9f1812609de4f0ce5ae3999840209b91a95f12e6035<br>
696fd7bd3222637e2f5f68857c2fad321cadfa71fc18ff13080b7b3e00705c25<br>
<br>
02、HEX转换成数字<br>
107726380326939678096191321759894641171889680277176394980912448220763383554101<br>
47690458101607612753575867337254955965309515698539539346868925906973493320741<br>
<br>
03、Secp256k1的压缩算法计算(02) <B>[ 公钥 ]</B><br>
03b713a951cca5cdac6cdade5a8147df3c219429b71ba78fb096e19e65b6c027ee<br>
03786cce8a843bbf62c55ee12cf08ead3e36034f5d360648c06bf7b06084bde835<br>
<br>
04、按以下顺序合并公钥HEX<br>
签名需要的私钥数量: 52 (80+需要的数量(1-6) 后转 byte)<br>
公钥1的HEX的长度byte: 21<br>
公钥1的HEX: 03b713a951cca5cdac6cdade5a8147df3c219429b71ba78fb096e19e65b6c027ee<br>
公钥2的HEX的长度byte: 21<br>
公钥2的HEX: 03786cce8a843bbf62c55ee12cf08ead3e36034f5d360648c06bf7b06084bde835<br>
公钥3-6（如果有)<br>
公钥总数量: 52 (80+公钥总数(1-6))<br>
常量: 0xae (OP_CHECKMULTISIG)<br>
组合结果: 522103b713a951cca5cdac6cdade5a8147df3c219429b71ba78fb096e19e65b6c027ee2103786cce8a843bbf62c55ee12cf08ead3e36034f5d360648c06bf7b06084bde83552ae<br>
<br>
05、SHA256计算公钥得到<br>
89dcc801bf3c5301c451bba101530e8428c79ca3cc3c1bf53eff1825e2ca8c1d<br>
<br>
06、RIPEMD160计算(05)的结果得到<br>
b6bc783aa94d7f80498bb359b81b21eee59e7232<br>
<br>
07、(06)的结果前加上版本0x05 (testnet 加 0xc4)<br>
05b6bc783aa94d7f80498bb359b81b21eee59e7232<br>
<br>
08、SHA256计算(07)的结果得到<br>
2dcd32ca2c3ae80ca7a81053d4bf71786cbc6cad92b247bd67e1474e577ade7e<br>
<br>
09、SHA256计算(08)的结果得到<br>
6910cb9c8b929a41698cc269ea2a8d4a1fa4da8833025c2ae19a1c697d1cadb8<br>
<br>
10、合并(07)和(09)的钱4个Byte<br>
05b6bc783aa94d7f80498bb359b81b21eee59e72326910cb9c<br>
<br>
11、BASE58计算(10)得到 <B>[ 地址 ]</B><br>
3JMEgZHMw1BkkwVQ7GUiXTjZaZKKeBanDh<br>
<br>
<br>

## SegWit Bech32 Address - P2WPKH - Pay to Witness Public Key Hash
---
