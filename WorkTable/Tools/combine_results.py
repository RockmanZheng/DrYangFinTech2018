from argparse import ArgumentParser

parser = ArgumentParser(description='Process some integers.')
parser.add_argument('results_path', type=str,
                    help='The path to results')
parser.add_argument('-', dest='accumulate', action='store_const',
                    const=sum, default=max,
                    help='sum the integers (default: find the max)')

args = parser.parse_args()
print(args.accumulate(args.integers))
