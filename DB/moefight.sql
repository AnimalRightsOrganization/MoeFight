/*
Navicat MySQL Data Transfer

Source Server         : localhost
Source Server Version : 50736
Source Host           : localhost:3306
Source Database       : moefight

Target Server Type    : MYSQL
Target Server Version : 50736
File Encoding         : 65001

Date: 2024-08-26 00:33:11
*/

SET FOREIGN_KEY_CHECKS=0;

-- ----------------------------
-- Table structure for tb_settings
-- ----------------------------
DROP TABLE IF EXISTS `tb_settings`;
CREATE TABLE `tb_settings` (
  `username` varchar(255) DEFAULT NULL,
  `screensize` tinyint(4) DEFAULT NULL,
  `fullscreen` tinyint(4) DEFAULT NULL,
  `audio` varchar(255) DEFAULT NULL,
  `sound` varchar(255) DEFAULT NULL,
  `language` varchar(255) DEFAULT NULL
) ENGINE=MyISAM DEFAULT CHARSET=gbk;

-- ----------------------------
-- Records of tb_settings
-- ----------------------------
INSERT INTO `tb_settings` VALUES ('test1', null, null, '0', '31', '0');
