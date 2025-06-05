from lxml import etree
import argparse
import os

parser = argparse.ArgumentParser()
parser.add_argument('fname')
args = parser.parse_args()

tree = etree.parse(args.fname)
new_fname = ('{}_fmt.lsr'.format(os.path.splitext(args.fname)[0]))
tree.write(new_fname, pretty_print=True)