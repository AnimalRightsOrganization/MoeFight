/*
Navicat MySQL Data Transfer

Source Server         : localhost
Source Server Version : 50736
Source Host           : localhost:3306
Source Database       : db_app

Target Server Type    : MYSQL
Target Server Version : 50736
File Encoding         : 65001

Date: 2024-08-26 00:33:01
*/

SET FOREIGN_KEY_CHECKS=0;

-- ----------------------------
-- Table structure for tb_app
-- ----------------------------
DROP TABLE IF EXISTS `tb_app`;
CREATE TABLE `tb_app` (
  `product_name` varchar(255) DEFAULT NULL,
  `web` varchar(255) DEFAULT NULL,
  `gate` varchar(255) DEFAULT NULL,
  `app_url` varchar(255) DEFAULT NULL,
  `res_url` varchar(255) DEFAULT NULL,
  `app_version` varchar(255) DEFAULT NULL,
  `res_version` varchar(255) DEFAULT NULL,
  `notice` varchar(255) DEFAULT NULL
) ENGINE=MyISAM DEFAULT CHARSET=gbk;

-- ----------------------------
-- Records of tb_app
-- ----------------------------
INSERT INTO `tb_app` VALUES ('moefight', 'moegijinka.cn', 'moegijinka.cn', 'moegijinka.cn', 'app.moegijinka.cn/moefight/res', '0.1.0', '1.0', '没有公告');
INSERT INTO `tb_app` VALUES ('turtlerace', 'moegijinka.cn', 'moegijinka.cn', 'moegijinka.cn', 'app.moegijinka.cn/turtlerace/res', '0.1.0', '1.0', '没有公告');
INSERT INTO `tb_app` VALUES ('afk', 'moegijinka.cn', 'moegijinka.cn', 'moegijinka.cn', 'app.moegijinka.cn/afk/res', '0.1.0', '1.0', '没有公告');
