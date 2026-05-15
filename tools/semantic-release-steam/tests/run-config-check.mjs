import plugin from '../index.mjs';

await plugin.verifyConditions(
  {
    branchTargets: { main: 'stable', beta: 'beta' },
    mods: [
      { name: 'Lightweave', path: '.', workshopIds: { stable: process.env.LIGHTWEAVE_STABLE, beta: process.env.LIGHTWEAVE_BETA } },
    ],
  },
  {
    branch: { name: process.env.BRANCH_NAME ?? 'beta' },
    env: process.env,
    logger: console,
  },
);

console.log('steam release config ok');
